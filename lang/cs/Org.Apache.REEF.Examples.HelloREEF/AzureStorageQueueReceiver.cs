using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Client.DotNet.AzureBatch
{
    public class AzureStorageQueueReceiver
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(AzureBatchDotNetClient));
        private readonly CloudQueue queue;

        public AzureStorageQueueReceiver()
        {

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("");

            // Create the queue client.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            queue = queueClient.GetQueueReference("responsequeue");
        }

        public void StartProcessingAsync()
        {
            Task t = Task.Run(async () =>
            {
                while (true)
                {
                    CloudQueueMessage message = await queue.GetMessageAsync();
                    if(message != null)
                    {
                        await queue.DeleteMessageAsync(message);
                    }
                    else
                    {
                        continue;
                    }
                    using (StreamWriter w = File.AppendText("C:\\Users\\chzha\\Desktop\\log.txt"))
                    {
                        w.WriteLine(message.AsString);
                    }
                    LOGGER.Log(Level.Info, message.AsString);
                }
            });
            t.Wait();
        }
    }
}
