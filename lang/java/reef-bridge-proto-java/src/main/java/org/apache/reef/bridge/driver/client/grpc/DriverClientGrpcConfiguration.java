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

package org.apache.reef.bridge.driver.client.grpc;

import org.apache.reef.annotations.Unstable;
import org.apache.reef.bridge.driver.client.DriverClientService;
import org.apache.reef.bridge.driver.client.DriverServiceClient;
import org.apache.reef.bridge.driver.client.grpc.parameters.DriverRegistrationTimeout;
import org.apache.reef.bridge.driver.client.grpc.parameters.DriverServicePort;
import org.apache.reef.tang.formats.ConfigurationModule;
import org.apache.reef.tang.formats.ConfigurationModuleBuilder;
import org.apache.reef.tang.formats.OptionalParameter;
import org.apache.reef.tang.formats.RequiredParameter;

/**
 * Configuration module for Grpc runtime.
 */
@Unstable
public final class DriverClientGrpcConfiguration extends ConfigurationModuleBuilder {

  public static final RequiredParameter<Integer> DRIVER_SERVICE_PORT = new RequiredParameter<>();

  public static final OptionalParameter<Integer> DRIVER_CLIENT_REGISTRATION_TIMEOUT = new OptionalParameter<>();

  public static final ConfigurationModule CONF = new DriverClientGrpcConfiguration()
      .bindImplementation(DriverClientService.class, GRPCDriverClientService.class)
      .bindImplementation(DriverServiceClient.class, GRPCDriverServiceClient.class)
      .bindNamedParameter(DriverServicePort.class, DRIVER_SERVICE_PORT)
      .bindNamedParameter(DriverRegistrationTimeout.class, DRIVER_CLIENT_REGISTRATION_TIMEOUT)
      .build();
}
