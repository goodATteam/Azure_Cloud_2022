using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Azure.ServiceBus;
using System;
using System.Text;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    const string ServiceBusConnectionString = "Endpoint=sb://eshopfinalservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=VSB0wRie6Xzf917+Ujln2GP6C6/uVl79puJgViMCAZk=";
    const string QueueName = "eshopfinalservicebus_queue";
    static IQueueClient queueClient;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        var outOrder = new
        {
            order.Id,
            order.OrderItems.Count
        };

        queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
        await SendMessageAsync(outOrder.ToString());
        await queueClient.CloseAsync();        

        var fUrl = "https://functionapp8820220514022830.azurewebsites.net/api/OrderItemReserver?";
        var httpCall = new HttpClient();
        var response = await httpCall.PostAsJsonAsync(fUrl, outOrder);
        var returnWait = await response.Content.ReadAsStringAsync();

        decimal finalPrice = 0;
        foreach (var ord in order.OrderItems)
        {
            finalPrice += ord.UnitPrice;
        }

        var shAdressFinally = shippingAddress.City + "," + shippingAddress.Country + "," + shippingAddress.Street;
        var itemsToJson = JsonSerializer.Serialize(items);
        var outOrderCosmoDB = JsonSerializer.Serialize(new
        {
            shAdressFinally,
            itemsToJson,
            finalPrice
        });

        var fUrlCosmoDB = "https://eshopfinalfunctionappcosmodb.azurewebsites.net/api/DeliveryOrderProcessor?";
        var httpCallCosmoDB = new HttpClient();
        var responseCosmoDB = await httpCallCosmoDB.PostAsJsonAsync(fUrlCosmoDB, outOrderCosmoDB);
        var returnWaitCosmoDB = await responseCosmoDB.Content.ReadAsStringAsync();

        await _orderRepository.AddAsync(order);
    }

    static async Task SendMessageAsync(string message)
    {
        try
        {
            var messageToSend = new Message(Encoding.UTF8.GetBytes(message));
            await queueClient.SendAsync(messageToSend);
        }
        catch (Exception exception)
        {
        }
    }
}
