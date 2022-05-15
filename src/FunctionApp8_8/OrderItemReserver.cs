using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.ServiceBus;
using System.Threading;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using System.Net.Http;

namespace FunctionApp8_8
{
    public static class OrderItemReserver
    {
        const string ServiceBusConnectionString = "Endpoint=sb://eshopfinalservicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=VSB0wRie6Xzf917+Ujln2GP6C6/uVl79puJgViMCAZk=";
        const string QueueName = "eshopfinalservicebus_queue";
        static IQueueClient queueClient;

        [FunctionName("OrderItemReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            MainAsync().GetAwaiter().GetResult();

            string responseMessage = "This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        static async Task MainAsync()
        {
            queueClient = new QueueClient(ServiceBusConnectionString, QueueName);
            RegisterOnMessageHandlerAndReceiveMessages();
            await queueClient.CloseAsync();
        }

        static void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceiveHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };
            queueClient.RegisterMessageHandler(
                ProcessMessagesAsync,
                messageHandlerOptions
                );
        }

        static async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            var messageToBlob = Encoding.UTF8.GetString(message.Body);

            SendToBlob(messageToBlob);

            Console.WriteLine(message.Body);
            await queueClient.CompleteAsync(message.SystemProperties.LockToken);
        }

        static async void SendToBlob(string messageToBlob)
        {
            var blobStorageUrl = "DefaultEndpointsProtocol=https;AccountName=eshopfinalsa;AccountKey=V4ix8ogUBTxiy8RAQkhcZq6eB5xrTRU+oKgxMCW+5vxFUUosaTOORZhYK/thbMthUwjB1c5aJQmv+AStx1k+WQ==;EndpointSuffix=core.windows.net";
            var blobServiceClient = new BlobServiceClient(blobStorageUrl);

            string containerName = "eshopfinalsablob";
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            containerClient.CreateIfNotExists();

            string name = Guid.NewGuid().ToString() + ".json";
            var blobClient = containerClient.GetBlobClient(name);

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(messageToBlob)))
            {
                await blobClient.UploadAsync(ms);
            }
        }

        static async Task ExceptionReceiveHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var client = new HttpClient();
            var jsonMessage = JsonSerializer.Serialize(
                new
                {
                    email = "Artur_Smaliuk@epam.com",
                    headBody = "eshopfinalservicebuss_mail",
                    bodyMessage = "Error inside"
                });

            var logicAppUrl = "https://prod-02.centralus.logic.azure.com:443/workflows/240059a31f6c41a393deabd4c12e56d5/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=sTkeTkxslWCGMSt_SGqiGaSsqxZfBLcu_Gmz105QF9k";
            HttpResponseMessage result = await client.PostAsync(logicAppUrl, new StringContent(jsonMessage, Encoding.UTF8, "application/json"));
        }
    }
}
