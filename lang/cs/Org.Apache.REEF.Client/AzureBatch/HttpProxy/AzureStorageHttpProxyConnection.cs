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
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Org.Apache.REEF.Common.Protobuf.ReefProtocol;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Client.DotNet.AzureBatch.HttpProxy
{
    /// <summary>
    /// The proxy class using Azure Storage Queue to communicate with Driver Http Server.
    /// </summary>
    public class AzureStorageHttpProxyConnection
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(AzureStorageHttpProxyConnection));
        private const string StorageConnectionStringFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
        private readonly CloudQueue _queue;
        private readonly string _storageAccountName;
        private readonly string _storageAccountKey;

        public AzureStorageHttpProxyConnection(string storageAccountName, string storageAccountKey, string queueName)
        {
            string storageConnectionString = string.Format(StorageConnectionStringFormat, new object[] { storageAccountName, storageAccountKey });

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(queueName);
            _storageAccountName = storageAccountName;
            _storageAccountKey = storageAccountKey;
        }

        /// <summary>
        /// Send Http proto request.
        /// </summary>
        /// <param name="request">A HttpProxyRequestProtol<see cref="HttpProxyRequestProtol"/> object represents the http request</param>
        /// <returns>request ID</returns>
        public async Task<string> SendMessageAsync(HttpProxyRequestProto request)
        {
            LOGGER.Log(Level.Info, "SendMessageAsync length: {0}", request.Serialize().Length);
            await _queue.AddMessageAsync(new CloudQueueMessage(Convert.ToBase64String(request.Serialize())));
            return request.id;
        }

        /// <summary>
        /// Send Http proto request and request response to be returned.
        /// </summary>
        /// <param name="request">A <see cref="HttpProxyRequestProtol"/> object represents the http request.</param>
        /// <returns>A <see cref="RequestMessageResponseAsync"/> object represents the http response</returns>
        public async Task<HttpProxyResponseProto> RequestMessageResponseAsync(HttpProxyRequestProto request)
        {
            // Use base64 encoding to work around https://github.com/Azure/azure-storage-net/issues/586, as REEF.Client is netstarndard 2.0 project.
            // Submitting Jobs with Azure-Storage .NET45 results in MethodNotFound Exception if CloudQueueMessage is created with byte[].
            await _queue.AddMessageAsync(new CloudQueueMessage(Convert.ToBase64String(request.Serialize())));
            AzureStorageHttpProxyInternalReceiver receiver = new AzureStorageHttpProxyInternalReceiver(_storageAccountName, _storageAccountKey, request.responseQueue);
            return await receiver.GetMessageAsync(request.id);
        }
    }
}
