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
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Org.Apache.REEF.Common.Protobuf.ReefProtocol;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Client.DotNet.AzureBatch.HttpProxy
{
    /// <summary>
    /// The proxy class using Azure Storage Queue to get response from Driver Http Server.
    /// </summary>
    internal class AzureStorageHttpProxyReceiver
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(AzureStorageHttpProxyReceiver));
        private const string StorageConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
        private const int BatchMessageMultiplier = 2;
        private const int BatchMessageMaxTryCount = 5;
        private readonly CloudQueue _queue;
        private readonly string _storageConnectionString;

        public AzureStorageHttpProxyReceiver(string storageAccountName, string storageAccountKey, string queueName)
        {

            _storageConnectionString = string.Format(StorageConnectionStringFormat, new object[] { storageAccountName, storageAccountKey });

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_storageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(queueName);
        }

        /// <summary>
        /// Get the response of Http proto request.
        /// </summary>
        /// <param name="id">A Guid that indentifies the Http proto request.</param>
        /// <returns>A <see cref="RequestMessageResponseAsync"/> object represents the http response</returns>
        public async Task<HttpProxyResponseProto> GetMessageAsync(string id)
        {
            int messageCount = 1;
            for (int i = 0; i < BatchMessageMaxTryCount; i++)
            {
                // There is a delay in Azure Storage Queue for the messages to be able ready to retrive, after adding messages.
                await Task.Delay(2000);
                IEnumerable<CloudQueueMessage> messages = await _queue.GetMessagesAsync(messageCount);
                foreach (CloudQueueMessage message in messages)
                {
                    byte[] responseData;
                    try
                    {    
                        // Protocol-buf-net thorws below excpetions when reading byte passed from Azure Storage Queue. Use base64 encoding to work around.
                        // Encountered error [ProtoBuf.ProtoException: Invalid wire-type; this usually means you have over-written a file without truncating or setting the length; see http://stackoverflow.com/q/2152978/23354.
                        responseData = Convert.FromBase64String(message.AsString);
                    }
                    catch (FormatException e)
                    {
                        LOGGER.Log(Level.Info, "Unable to convert the response: " + e);
                        continue;
                    }

                    HttpProxyResponseProto response = HttpProxyResponseProto.Deserialize(responseData);
                    LOGGER.Log(Level.Info, "GetMessageAsync response id: {0}, response code: {1}, response body: {2}", new object[] { response.id, response.responseCode, response.responseBody });
                    if (response != null && response.id.Equals(id))
                    {
                        await _queue.DeleteMessageAsync(message);
                        return response;
                    }
                }

                messageCount *= BatchMessageMultiplier;
            }

            return null;
        }
    }
}
