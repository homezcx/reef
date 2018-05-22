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
using Org.Apache.REEF.Utilities.Logging;
using Org.Apache.REEF.Client.API;
using Org.Apache.REEF.Common.Protobuf.ReefProtocol;
using System.Threading.Tasks;
using Org.Apache.REEF.Client.Common.HttpProxy;
using System.Diagnostics.CodeAnalysis;

namespace Org.Apache.REEF.Client.Common
{
    internal class AzureStorageQueueJobSubmissionResult : JobSubmissionResult
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(AzureStorageQueueJobSubmissionResult));
        private readonly AzureStorageQueueHttpConnectionProxy _proxy;
        private readonly string _defaultResponseName;
        private readonly int _numberOfRetries;
        private readonly int _retryInterval;

        internal AzureStorageQueueJobSubmissionResult(IREEFClient reefClient,
            int retryInterval,
            int numberOfRetries,
            string azureStorageAccountName,
            string azureStorageAccountKey,
            string azureStorageHttpProxyRequestQueueName,
            string azureStorageHttpProxyDefaultResponseQueueName)
               : base(reefClient, default(string), numberOfRetries, retryInterval)
        {
            _proxy = new AzureStorageQueueHttpConnectionProxy(azureStorageAccountName, azureStorageAccountKey, azureStorageHttpProxyRequestQueueName);
            _defaultResponseName = azureStorageHttpProxyDefaultResponseQueueName;
            _numberOfRetries = numberOfRetries;
            _retryInterval = retryInterval;
        }

        protected override string GetDriverUrl(string filepath)
        {
            // Azure Batch is not able to communicate to driver through driver endpoint. Leave this interface as empty.
            // TODO: Expect improvement by [REEF-2020]
            return default(string);
        }

        protected override DriverStatus FetchFirstDriverStatus()
        {
            for (int i = 0; i < _numberOfRetries; i++)
            {
                DriverStatus status = FetchDriverStatus();
                if (!DriverStatus.UNKNOWN.Equals(status))
                {
                    return status;
                }

                Task.Delay(TimeSpan.FromMilliseconds(_retryInterval)).GetAwaiter().GetResult();
            }
            return DriverStatus.UNKNOWN;
        }

        protected override DriverStatus FetchDriverStatus()
        {
            HttpProxyResponseProto response = _proxy.RequestMessageResponseAsync(new HttpProxyRequestProto()
            {
                endpoint = "driverstatus/v1",
                id = Guid.NewGuid().ToString(),
                responseQueue = _defaultResponseName
            }).GetAwaiter().GetResult();

            if (response == null)
            {
                return DriverStatus.UNKNOWN;
            }

            string statusString = response.responseBody;
            LOGGER.Log(Level.Verbose, "Status received: {0}", statusString);
            return DriverStatusMethods.Parse(statusString);
        }
    }
}
