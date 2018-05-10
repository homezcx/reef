/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
package org.apache.reef.javabridge.generic;

import com.microsoft.windowsazure.storage.CloudStorageAccount;
import com.microsoft.windowsazure.storage.StorageException;
import com.microsoft.windowsazure.storage.queue.CloudQueue;
import com.microsoft.windowsazure.storage.queue.CloudQueueClient;
import com.microsoft.windowsazure.storage.queue.CloudQueueMessage;
import org.apache.commons.codec.binary.Base64;

import java.io.IOException;
import java.net.URISyntaxException;
import java.security.InvalidKeyException;
import java.util.logging.Level;
import java.util.logging.Logger;

import static org.apache.reef.proto.azurebatch.ReefAzureBatchHttpProtos.*;

/**
 * TODO: Task 244273
 */
public class HttpRequestProxy {

  private static final Logger LOG = Logger.getLogger(HttpRequestProxy.class.getName());
  private final String connectionString = "###";
  private final String queueName = "###";
  private final CloudQueue messageQueue;
  private final HttpResponseProxy httpResponseProxy;

  public HttpRequestProxy(final String serverHostString) {
    // Retrieve storage account from connection-string.

    try {
      CloudStorageAccount storageAccount = CloudStorageAccount.parse(connectionString);

      // Create the queue client.
      CloudQueueClient queueClient = storageAccount.createCloudQueueClient();

      // Retrieve a reference to a queue.
      this.messageQueue = queueClient.getQueueReference(queueName);

    } catch (URISyntaxException | InvalidKeyException | StorageException e) {
      e.printStackTrace();
      throw new RuntimeException("CloudQueue cannot be initialized. " + e);
    }

    this.httpResponseProxy = new HttpResponseProxy(serverHostString);
  }

  public void startProcessing(final String serverHostString) {
    LOG.log(Level.INFO, "startProcessing serverString " + serverHostString);
    new Thread() {
      @Override
      public void run() {
        while (true) {
          try {
            Thread.sleep(5000);
            CloudQueueMessage message = messageQueue.retrieveMessage();
            if (message != null) {
              messageQueue.deleteMessage(message);
            } else {
              continue;
            }
            LOG.log(Level.INFO, "retrieveMessage " + message.getMessageContentAsString());
            // Use base64 encoding to work around https://github.com/Azure/azure-storage-net/issues/586, as REEF.Client is netstarndard 2.0 project.
            // Submitting Jobs with Azure-Storage .NET45 results in MethodNotFound Exception if CloudQueueMessage is created with byte[].
            HttpProxyRequestProto request = HttpProxyRequestProto.parseFrom(Base64.decodeBase64(message.getMessageContentAsByte()));
            httpResponseProxy.sendResponse(request);
          } catch (StorageException | InterruptedException | IOException e) {
            e.printStackTrace();
            throw new RuntimeException("Http request cannot be sent. " + e);
          }
        }
      }
    }.start();
  }
}
