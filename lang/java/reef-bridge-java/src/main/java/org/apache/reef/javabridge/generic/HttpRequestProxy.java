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

import javax.inject.Inject;
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
 *
 */
public class HttpRequestProxy {

  private static final Logger LOG = Logger.getLogger(HttpRequestProxy.class.getName());
  private final String connectionString = "";
  private final String queueName = "requestqueue";
  private final CloudQueue messageQueue;
  private final HttpResponseProxy httpResponseProxy;

  @Inject
  HttpRequestProxy(final HttpResponseProxy httpResponseProxy) {
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

    this.httpResponseProxy = httpResponseProxy;
  }

  public void startProcessing(final String serverString) {
    LOG.log(Level.INFO, "startProcessing serverString " + serverString);
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

            LOG.log(Level.INFO, "HttpRequestProxy receives " + message.getMessageContentAsString());
            HttpURLConnection connection = (HttpURLConnection) (new URL(serverString + '/' + message.getMessageContentAsString()).openConnection());
            LOG.log(Level.INFO, "HttpRequestProxy response code " + connection.getResponseCode());
            // Check response code
            BufferedReader br;
            if (HttpServletResponse.SC_OK == connection.getResponseCode()) {
              br = new BufferedReader(new InputStreamReader(connection.getInputStream()));
            } else {
              br = new BufferedReader(new InputStreamReader(connection.getErrorStream()));
            }

            String output;
            StringBuilder messageBuffer = new StringBuilder();
            while (( output = br.readLine()) !=null){
              messageBuffer.append(output);
            }
            LOG.log(Level.INFO, "HttpRequestProxy response body " + messageBuffer.toString());
            httpResponseProxy.sendMessage(messageBuffer.toString());
          } catch (StorageException | InterruptedException | IOException e) {
            e.printStackTrace();
            throw new RuntimeException("Http request cannot be sent. " + e);
          }
        }
      }
    }.start();
  }
}
