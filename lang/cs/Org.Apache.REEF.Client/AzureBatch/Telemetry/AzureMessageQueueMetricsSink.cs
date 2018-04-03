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

using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Org.Apache.REEF.Tang.Annotations;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Common.Telemetry
{
    /// <summary>
    /// This default IMetricsSink is just an example of IMetricsSink
    /// Here the data is logged in Sink() method
    /// It is more useful in test
    /// </summary>
    internal sealed class AzureMessageQueueMetricsSink : IMetricsSink
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(AzureMessageQueueMetricsSink));
        //// TODO: replace hard coded credential
        const string ServiceBusConnectionString = @"###################################";
        const string QueueName = @"##############################";
        private QueueClient queueClient;

        [Inject]
        private AzureMessageQueueMetricsSink()
        {
            queueClient = QueueClient.CreateFromConnectionString(ServiceBusConnectionString, QueueName);
        }

        /// <summary>
        /// Simple sink for metrics data
        /// </summary>
        /// <param name="metrics">A collection of metrics data in Key value pair format.</param>
        public async void Sink(IEnumerable<KeyValuePair<string, string>> metrics)
        {
            foreach (var m in metrics)
            {
                string message = string.Format("Metrics - Name:{0}, Value:{1}.", m.Key, m.Value);
                Logger.Log(Level.Info, message);
                await queueClient.SendAsync(new BrokeredMessage(message));
            }
        }

        /// <summary>
        /// This is intentionally empty as we don't have any resource to release in the implementation.
        /// </summary>
        public void Dispose()
        {
        }
    }
}