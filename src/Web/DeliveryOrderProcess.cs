namespace Microsoft.eShopWeb.Web;

public class DeliveryOrderProcess
{
    //Insert special code to azure portal for function with name DeliveryOrderProcess
}

/*
 #r "Newtonsoft.Json"

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json;

public static IActionResult Run(HttpRequest req, out object outputOrder, ILogger log)
{
    string shippingAddress = req.Query["shippingAddress"];
    string items = req.Query["items"];
    string finalPrice = req.Query["finalPrice"];

    string requestBody = new StreamReader(req.Body).ReadToEnd();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    shippingAddress = shippingAddress ?? data?.shippingAddress;
    items = items ?? data?.items;
    finalPrice = finalPrice ?? data?.finalPrice;

    if (!string.IsNullOrEmpty(items) && !string.IsNullOrEmpty(shippingAddress) && !string.IsNullOrEmpty(finalPrice))
    {
        outputOrder = new
        {
            shippingAddress,
            items,
            finalPrice
        };

        return new OkResult();
    }
    else
    {
        outputOrder = null;
        return new BadRequestResult();
    }
}
 */
