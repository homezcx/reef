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

import javax.inject.Inject;
import java.net.URISyntaxException;
import java.security.InvalidKeyException;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 *
 */
public class HttpResponseProxy {

  private static final Logger LOG = Logger.getLogger(HttpResponseProxy.class.getName());
  private final String connectionString = "";
  private final String queueName = "responsequeue";
  private final CloudQueue messageQueue;

  @Inject
  HttpResponseProxy() {
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

  public void sendMessage(String message){
    try {
      LOG.log(Level.INFO, "HttpResponseProxy sends " + message);
      this.messageQueue.addMessage(new CloudQueueMessage(message));
    } catch (StorageException e) {
      e.printStackTrace();
      throw new RuntimeException("CloudQueue cannot be initialized. " + e);
    }
  }
}
