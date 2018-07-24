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
using System.Net;
using Org.Apache.REEF.Common.Telemetry;
using Org.Apache.REEF.Driver.Bridge;
using Org.Apache.REEF.Tang.Annotations;
using Org.Apache.REEF.Utilities;
using Org.Apache.REEF.Utilities.Logging;

namespace Org.Apache.REEF.Driver.Telemetry
{
    public class MetricsStatusHandler : IHttpHandler
    {
        private static readonly Logger LOGGER = Logger.GetLogger(typeof(MetricsStatusHandler));
        private readonly MetricsService _service;

        [Inject]
        private MetricsStatusHandler(MetricsService service)
        {
            _service = service;
        }

        public string GetSpecification()
        {
            return "metrics";
        }

        public void OnHttpRequest(ReefHttpRequest requet, ReefHttpResponse response)
        {
            LOGGER.Log(Level.Info, "OnHttpRequest in MetricsStatusHandler is called.");
            response.Status = HttpStatusCode.OK;
            response.OutputStream = ByteUtilities.StringToByteArrays(_service.DumpMetrics());
        }
    }
}
