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
package org.apache.reef.bridge.client;

import com.microsoft.windowsazure.storage.CloudStorageAccount;
import com.microsoft.windowsazure.storage.StorageException;
import com.microsoft.windowsazure.storage.queue.CloudQueue;
import com.microsoft.windowsazure.storage.queue.CloudQueueClient;
import com.microsoft.windowsazure.storage.queue.CloudQueueMessage;
import org.apache.commons.codec.binary.Base64;
import org.apache.reef.proto.azurebatch.ReefAzureBatchHttpProtos;
import org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountKey;
import org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountName;
import org.apache.reef.runtime.azbatch.parameters.AzureStorageHttpProxyRequestQueueName;
import org.apache.reef.runtime.common.files.REEFFileNames;
import org.apache.reef.tang.annotations.Parameter;

import javax.inject.Inject;
import javax.servlet.http.HttpServletResponse;
import java.io.BufferedReader;
import java.io.File;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URISyntaxException;
import java.net.URL;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.security.InvalidKeyException;
import java.util.logging.Level;
import java.util.logging.Logger;

import static org.apache.reef.proto.azurebatch.ReefAzureBatchHttpProtos.HttpProxyRequestProto;

/**
 * The proxy class using Azure Storage Queue to get response from Driver Http Server and sent to client.
 */
public final class AzureStorageHttpConnectionProxy {

  private static final Logger LOG = Logger.getLogger(AzureStorageHttpConnectionProxy.class.getName());
  private final String connectionStringFormat =
      "DefaultEndpointsProtocol=https;AccountName=%s;AccountKey=%s;EndpointSuffix=core.windows.net";
  private final CloudQueue requestQueue;
  private final REEFFileNames reefFileNames;
  private final String connectionString;
  private String serverHostString;

  @Inject
  AzureStorageHttpConnectionProxy(
      @Parameter(AzureStorageAccountName.class) final String storageAccountName,
      @Parameter(AzureStorageAccountKey.class) final String storageAccountKey,
      @Parameter(AzureStorageHttpProxyRequestQueueName.class) final String queueName,
      final REEFFileNames reefFileNames) {

    this.connectionString = String.format(connectionStringFormat, storageAccountName, storageAccountKey);

    try {
      CloudStorageAccount storageAccount = CloudStorageAccount.parse(this.connectionString);
      CloudQueueClient queueClient = storageAccount.createCloudQueueClient();
      this.requestQueue = queueClient.getQueueReference(queueName);
    } catch (URISyntaxException | InvalidKeyException | StorageException e) {
      throw new RuntimeException("CloudQueue cannot be initialized. " + e);
    }

    this.reefFileNames = reefFileNames;
  }

  public void startProcessing() {
    new Thread() {
      @Override
      public void run() {
        while (true) {
          try {
            Thread.sleep(2000);
            if (AzureStorageHttpConnectionProxy.this.serverHostString == null) {
              final String driverHttpEndPointFile =
                  AzureStorageHttpConnectionProxy.this.reefFileNames.getDriverHttpEndpoint();
              final File outputFileName = new File(driverHttpEndPointFile);
              if (outputFileName.exists()) {
                String endpoint = new String(Files.readAllBytes(Paths.get(driverHttpEndPointFile)));
                // remove '\n' in the string
                AzureStorageHttpConnectionProxy.this.serverHostString = endpoint.substring(0, endpoint.length() - 1);
              } else {
                continue;
              }
            }

            CloudQueueMessage message = requestQueue.retrieveMessage();
            if (message != null) {
              requestQueue.deleteMessage(message);
            } else {
              continue;
            }
            LOG.log(Level.INFO, "retrieveMessage " + message.getMessageContentAsString());
            // Use base64 encoding to work around https://github.com/Azure/azure-storage-net/issues/586,
            // as REEF.Client is netstandard 2.0 project. Submitting Jobs with Azure-Storage .NET45 results
            // in MethodNotFound Exception if CloudQueueMessage is created with byte[].
            HttpProxyRequestProto request = HttpProxyRequestProto.parseFrom(
                Base64.decodeBase64(message.getMessageContentAsByte()));

            AzureStorageHttpConnectionProxy.this.requestAndForwardResponse(request);

          } catch (StorageException | InterruptedException | IOException e) {
            throw new RuntimeException("Http request cannot be sent. " + e);
          }
        }
      }
    }.start();
  }

  private void requestAndForwardResponse(HttpProxyRequestProto request) throws IOException, StorageException {
    LOG.log(Level.INFO, "AzureStorageHttpConnectionProxy receives " + request.toString());
    HttpURLConnection connection = (HttpURLConnection) (
        new URL(this.serverHostString + '/' + request.getEndpoint()).openConnection());
    int responseCode = connection.getResponseCode();

    // Check response code
    BufferedReader br;
    if (HttpServletResponse.SC_OK == connection.getResponseCode()) {
      br = new BufferedReader(new InputStreamReader(connection.getInputStream()));
    } else {
      br = new BufferedReader(new InputStreamReader(connection.getErrorStream()));
    }

    String responseBody;
    StringBuilder messageBuffer = new StringBuilder();
    while ((responseBody = br.readLine()) != null) {
      messageBuffer.append(responseBody);
    }
    LOG.log(Level.INFO, "AzureStorageHttpConnectionProxy response body " + messageBuffer.toString());

    ReefAzureBatchHttpProtos.HttpProxyResponseProto response = ReefAzureBatchHttpProtos.HttpProxyResponseProto.newBuilder()
        .setId(request.getId())
        .setResponseCode(responseCode)
        .setResponseBody(messageBuffer.toString())
        .build();

    final CloudQueue responseQueue;
    try {
      CloudStorageAccount storageAccount = CloudStorageAccount.parse(this.connectionString);
      CloudQueueClient queueClient = storageAccount.createCloudQueueClient();
      responseQueue = queueClient.getQueueReference(request.getResponseQueue());
    } catch (URISyntaxException | InvalidKeyException | StorageException e) {
      throw new RuntimeException("CloudQueue cannot be initialized. " + e);
    }

    // Protocol-buf-net throws below exceptions when reading byte passed from Azure Storage Queue. Use base64 encoding to work around.
    // Encountered error [ProtoBuf.ProtoException: Invalid wire-type; this usually means you have over-written a file without truncating or setting the length; see http://stackoverflow.com/q/2152978/23354.
    responseQueue.addMessage(new CloudQueueMessage(Base64.encodeBase64(response.toByteArray())));
  }
}
