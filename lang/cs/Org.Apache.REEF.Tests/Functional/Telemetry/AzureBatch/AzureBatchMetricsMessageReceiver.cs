// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
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
