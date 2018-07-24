﻿// Licensed to the Apache Software Foundation (ASF) under one
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
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Org.Apache.REEF.IO.FileSystem.AzureBlob.Parameters;
using Org.Apache.REEF.IO.FileSystem.AzureBlob.RetryPolicy;
using Org.Apache.REEF.Tang.Annotations;

namespace Org.Apache.REEF.IO.FileSystem.AzureBlob
{
    /// <summary>
    /// A proxy class for CloudBlobClient, mainly in order to fake for unit testing.
    /// </summary>
    internal sealed class AzureCloudBlobClient : ICloudBlobClient
    {
        private readonly CloudBlobClient _client;
        private const string AzureBlobConnectionFormat = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};";
        private readonly BlobRequestOptions _requestOptions;

        public StorageCredentials Credentials 
        { 
            get { return _client.Credentials; } 
        }

        [Inject]
        private AzureCloudBlobClient([Parameter(typeof(AzureBlobStorageAccountName))] string accountName,
                                     [Parameter(typeof(AzureBlobStorageAccountKey))] string accountKey,
                                     IAzureBlobRetryPolicy retryPolicy)
        {
            var connectionString = string.Format(AzureBlobConnectionFormat, accountName, accountKey);
            _client = CloudStorageAccount.Parse(connectionString).CreateCloudBlobClient();
            _client.DefaultRequestOptions.RetryPolicy = retryPolicy;
            _requestOptions = new BlobRequestOptions() { RetryPolicy = retryPolicy };
        }

        public Uri BaseUri
        {
            get { return _client.BaseUri; }
        }

        public ICloudBlob GetBlobReferenceFromServer(Uri blobUri)
        {
            var task = _client.GetBlobReferenceFromServerAsync(blobUri);
            return task.Result;
        }

        public ICloudBlobContainer GetContainerReference(string containerName)
        {
            return new AzureCloudBlobContainer(_client.GetContainerReference(containerName), _requestOptions);
        }

        public ICloudBlockBlob GetBlockBlobReference(Uri uri)
        {
            return new AzureCloudBlockBlob(uri, _client.Credentials, _requestOptions);
        }

        public BlobResultSegment ListBlobsSegmented(
            string containerName,
            string relativeAddress,
            bool useFlatListing,
            BlobListingDetails blobListingDetails,
            int? maxResults,
            BlobContinuationToken continuationToken,
            BlobRequestOptions blobRequestOptions,
            OperationContext operationContext)
        {
            CloudBlobContainer container = _client.GetContainerReference(containerName);
            CloudBlobDirectory directory = container.GetDirectoryReference(relativeAddress);
            return directory.ListBlobsSegmentedAsync(
                useFlatListing,
                blobListingDetails,
                maxResults,
                continuationToken,
                blobRequestOptions,
                operationContext).GetAwaiter().GetResult();
        }

        public ContainerResultSegment ListContainersSegmented(BlobContinuationToken continuationToken)
        {
            return _client.ListContainersSegmentedAsync(continuationToken).GetAwaiter().GetResult();
        }
    }
}
