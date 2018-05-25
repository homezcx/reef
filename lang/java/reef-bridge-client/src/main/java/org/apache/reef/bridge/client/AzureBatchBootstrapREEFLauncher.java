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

import com.microsoft.azure.batch.BatchClient;
import com.microsoft.azure.batch.auth.BatchCredentials;
import com.microsoft.azure.batch.protocol.models.InboundNATPool;
import com.microsoft.azure.batch.protocol.models.NetworkConfiguration;
import com.microsoft.azure.batch.protocol.models.PoolEndpointConfiguration;
import org.apache.avro.io.DecoderFactory;
import org.apache.avro.io.JsonDecoder;
import org.apache.avro.specific.SpecificDatumReader;
import org.apache.reef.annotations.audience.Interop;
import org.apache.reef.reef.bridge.client.avro.AvroAzureBatchJobSubmissionParameters;
import org.apache.reef.runtime.azbatch.client.AzureBatchRuntimeConfiguration;
import org.apache.reef.runtime.azbatch.client.AzureBatchRuntimeConfigurationCreator;
import org.apache.reef.runtime.azbatch.parameters.AzureBatchPoolId;
import org.apache.reef.runtime.azbatch.util.batch.SharedKeyBatchCredentialProvider;
import org.apache.reef.runtime.common.REEFEnvironment;
import org.apache.reef.runtime.common.evaluator.PIDStoreStartHandler;
import org.apache.reef.runtime.common.launch.REEFErrorHandler;
import org.apache.reef.runtime.common.launch.REEFMessageCodec;
import org.apache.reef.tang.*;
import org.apache.reef.tang.exceptions.InjectionException;
import org.apache.reef.wake.remote.RemoteConfiguration;
import org.apache.reef.wake.remote.ports.ArrayTcpPortProvider;
import org.apache.reef.wake.remote.ports.TcpPortProvider;
import org.apache.reef.wake.remote.ports.parameters.TcpPortArray;
import org.apache.reef.wake.time.Clock;

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import java.util.logging.Level;
import java.util.logging.Logger;

/**
 * This is a bootstrap launcher for Azure Batch for submission from C#. It allows for Java Driver
 * configuration generation directly on the Driver without need of Java dependency if REST
 * submission is used. Note that the name of the class must contain "REEFLauncher" for the time
 * being in order for the Interop code to discover the class.
 */
@Interop(CppFiles = "DriverLauncher.cpp")
public final class AzureBatchBootstrapREEFLauncher {

  private static final Logger LOG = Logger.getLogger(AzureBatchBootstrapREEFLauncher.class.getName());
  private static final Tang TANG = Tang.Factory.getTang();

  public static void main(final String[] args) throws IOException, InjectionException {

    LOG.log(Level.INFO, "Entering BootstrapLauncher.main(). {0}", args[0]);

    if (args.length != 1) {

      final StringBuilder sb = new StringBuilder(
          "Bootstrap launcher should have one configuration file input," +
          " specifying the job submission parameters to be deserialized" +
          " to create the Azure Batch DriverConfiguration on the fly." +
          " Current args are [ ");
      for (String arg : args) {
        sb.append(arg).append(" ");
      }
      sb.append("]");

      final String message = sb.toString();
      throw fatal(message, new IllegalArgumentException(message));
    }

    final File partialConfigFile = new File(args[0]);
    final Injector injector = TANG.newInjector(generateConfigurationFromJobSubmissionParameters(partialConfigFile));
    final AzureBatchBootstrapDriverConfigGenerator azureBatchBootstrapDriverConfigGenerator =
        injector.getInstance(AzureBatchBootstrapDriverConfigGenerator.class);

    final JavaConfigurationBuilder launcherConfigBuilder =
        TANG.newConfigurationBuilder()
            .bindNamedParameter(RemoteConfiguration.ManagerName.class, "AzureBatchBootstrapREEFLauncher")
            .bindNamedParameter(RemoteConfiguration.ErrorHandler.class, REEFErrorHandler.class)
            .bindNamedParameter(RemoteConfiguration.MessageCodec.class, REEFMessageCodec.class)
            .bindSetEntry(Clock.RuntimeStartHandler.class, PIDStoreStartHandler.class);

    final List<String> availablePorts = getAzureBatchInBoundNatPoolBackendPorts(
        injector.getInstance(SharedKeyBatchCredentialProvider.class).getCredentials(),
        injector.getNamedInstance(AzureBatchPoolId.class));

    if (availablePorts != null) {
      launcherConfigBuilder.bindList(TcpPortArray.class, availablePorts)
          .bindImplementation(TcpPortProvider.class, ArrayTcpPortProvider.class);
    }

    final Configuration launcherConfig = launcherConfigBuilder.build();

    try (final REEFEnvironment reef = REEFEnvironment.fromConfiguration(
        azureBatchBootstrapDriverConfigGenerator.getDriverConfigurationFromParams(args[0]), launcherConfig)) {
      reef.run();
    } catch (final InjectionException ex) {
      throw fatal("Unable to configure and start REEFEnvironment.", ex);
    }

    LOG.log(Level.INFO, "Exiting BootstrapLauncher.main()");

    System.exit(0); // TODO[REEF-1715]: Should be able to exit cleanly at the end of main()
  }

  private static Configuration generateConfigurationFromJobSubmissionParameters(final File params) throws IOException {

    final AvroAzureBatchJobSubmissionParameters avroAzureBatchJobSubmissionParameters;

    try (final FileInputStream fileInputStream = new FileInputStream(params)) {
      final JsonDecoder decoder = DecoderFactory.get().jsonDecoder(
          AvroAzureBatchJobSubmissionParameters.getClassSchema(), fileInputStream);
      final SpecificDatumReader<AvroAzureBatchJobSubmissionParameters> reader =
          new SpecificDatumReader<>(AvroAzureBatchJobSubmissionParameters.class);
      avroAzureBatchJobSubmissionParameters = reader.read(null, decoder);
    }

    return AzureBatchRuntimeConfigurationCreator
        .getOrCreateAzureBatchRuntimeConfiguration(avroAzureBatchJobSubmissionParameters.getAzureBatchIsWindows())
        .set(AzureBatchRuntimeConfiguration.AZURE_BATCH_ACCOUNT_NAME,
            avroAzureBatchJobSubmissionParameters.getAzureBatchAccountName().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_BATCH_ACCOUNT_KEY,
            avroAzureBatchJobSubmissionParameters.getAzureBatchAccountKey().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_BATCH_ACCOUNT_URI,
            avroAzureBatchJobSubmissionParameters.getAzureBatchAccountUri().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_BATCH_POOL_ID,
            avroAzureBatchJobSubmissionParameters.getAzureBatchPoolId().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_STORAGE_ACCOUNT_NAME,
            avroAzureBatchJobSubmissionParameters.getAzureStorageAccountName().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_STORAGE_ACCOUNT_KEY,
            avroAzureBatchJobSubmissionParameters.getAzureStorageAccountKey().toString())
        .set(AzureBatchRuntimeConfiguration.AZURE_STORAGE_CONTAINER_NAME,
            avroAzureBatchJobSubmissionParameters.getAzureStorageContainerName().toString())
        .build();
  }

  private static List<String> getAzureBatchInBoundNatPoolBackendPorts(BatchCredentials credentials, String poolId) {
    final BatchClient client = BatchClient.open(credentials);
    final NetworkConfiguration networkConfiguration;

    try {
      networkConfiguration = client.poolOperations().getPool(poolId).networkConfiguration();
    } catch (IOException e) {
      LOG.log(Level.WARNING, "unable to setup Http Server with InBoundNatPool Port", e);
      return null;
    }

    if (networkConfiguration == null) {
      return null;
    }

    final PoolEndpointConfiguration endpointConfiguration = networkConfiguration.endpointConfiguration();
    if (endpointConfiguration == null) {
      return null;
    }

    final List<InboundNATPool> inboundNATpools = endpointConfiguration.inboundNATPools();
    final List<String> backendPorts = new ArrayList();
    for (InboundNATPool pool : inboundNATpools) {
      backendPorts.add(String.valueOf(pool.backendPort()));
    }
    return backendPorts;
  }

  private static RuntimeException fatal(final String msg, final Throwable t) {
    LOG.log(Level.SEVERE, msg, t);
    return new RuntimeException(msg, t);
  }

  private AzureBatchBootstrapREEFLauncher() {
  }
}
