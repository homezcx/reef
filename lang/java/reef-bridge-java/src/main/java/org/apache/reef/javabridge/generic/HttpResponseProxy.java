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

import com.microsoft.windowsazure.storage.*;
import com.microsoft.windowsazure.storage.queue.*;
import org.apache.commons.codec.binary.Base64;
import org.apache.reef.proto.azurebatch.ReefAzureBatchHttpProtos.HttpProxyRequestProto;
import org.apache.reef.proto.azurebatch.ReefAzureBatchHttpProtos.HttpProxyResponseProto;

import javax.servlet.http.HttpServletResponse;
import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URISyntaxException;
import java.net.URL;
import java.security.InvalidKeyException;
import java.util.logging.Level;
import java.util.logging.Logger;


/**
 * TODO: Task 244273
 */
public class HttpResponseProxy {

  private static final Logger LOG = Logger.getLogger(HttpResponseProxy.class.getName());
  private final String connectionString = "###";
  private final String queueName = "###";
  private final CloudQueue messageQueue;
  private final String serverHostString;

  public HttpResponseProxy(final String serverHostString) {

    this.serverHostString = serverHostString;

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
  }

  public void sendResponse(HttpProxyRequestProto request) throws IOException, StorageException {

    LOG.log(Level.INFO, "HttpRequestProxy receives " + request.toString());
    HttpURLConnection connection = (HttpURLConnection) (new URL(this.serverHostString + '/' + request.getEndpoint()).openConnection());
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
    while (( responseBody = br.readLine()) !=null){
      messageBuffer.append(responseBody);
    }
    LOG.log(Level.INFO, "HttpRequestProxy response body " + messageBuffer.toString());

    HttpProxyResponseProto response = HttpProxyResponseProto.newBuilder()
        .setId(request.getId())
        .setResponseCode(responseCode)
        .setResponseBody(messageBuffer.toString())
        .build();

    // Protocol-buf-net thorws below excpetions when reading byte passed from Azure Storage Queue. Use base64 encoding to work around.
    // Encountered error [ProtoBuf.ProtoException: Invalid wire-type; this usually means you have over-written a file without truncating or setting the length; see http://stackoverflow.com/q/2152978/23354.
    this.messageQueue.addMessage(new CloudQueueMessage(Base64.encodeBase64(response.toByteArray())));
  }
}
