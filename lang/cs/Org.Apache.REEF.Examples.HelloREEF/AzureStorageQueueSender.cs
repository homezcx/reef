using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Examples.HelloREEF.DotNet
{
    public class AzureStorageQueueSender
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(AzureStorageQueueSender));
        private readonly CloudQueue queue;

        public AzureStorageQueueSender()
        {

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("");

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            queue = queueClient.GetQueueReference("requestqueue");
        }

        public async Task SendMessageAsync(string queryString)
        {
            await queue.AddMessageAsync(new CloudQueueMessage(queryString));
        }
    }
}
