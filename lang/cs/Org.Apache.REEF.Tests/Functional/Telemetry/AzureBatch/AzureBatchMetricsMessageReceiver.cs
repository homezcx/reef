using System;
using System.IO;
using Microsoft.ServiceBus.Messaging;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Tests.Functional.Telemetry.AzureBatch
{
    internal sealed class AzureBatchMetricsMessageReceiver
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(AzureBatchMetricsMessageReceiver));
        //// TODO: replace hard coded credential
        private const string serviceBusConnectionString = @"######################################";
        private const string queueName = @"###################################";
        private QueueClient queueClient;
        private string logPath;

        public AzureBatchMetricsMessageReceiver(string logPath)
        {
            queueClient = QueueClient.CreateFromConnectionString(serviceBusConnectionString, queueName);
            this.logPath = logPath;
        }

        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new OnMessageOptions()
            {
                MaxConcurrentCalls = 1,

                AutoComplete = false,
            };

            messageHandlerOptions.ExceptionReceived += ExceptionReceivedHandler;

            // Register the function that processes messages.
            queueClient.OnMessage(ProcessMessagesAsync, messageHandlerOptions);
        }

        private async void ProcessMessagesAsync(BrokeredMessage message)
        {
            WriteLogFile(message.GetBody<string>());
            await queueClient.CompleteAsync(message.LockToken);
        }

        private void ExceptionReceivedHandler(object sender, ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
        }

        private void WriteLogFile(string message)
        {
            using (StreamWriter w = File.AppendText(logPath))
            {
                w.WriteLine(message);
            }
        }

        public string[] ReadLogFile()
        {
            return File.ReadAllLines(logPath);
        }
    }
}
