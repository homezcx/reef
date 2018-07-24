using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.REEF.Common.Metrics.MetricsSystem;
using Org.Apache.REEF.Tang.Annotations;

namespace Org.Apache.REEF.Tests.Functional.Telemetry
{
    public class TestMetricsDataHandler : IMetricsDataHandler
    {
        [Inject]
        private TestMetricsDataHandler()
        {

        }

        public string OnMetricsData(string rawData)
        {
            return "test injection OnMetricsData \n" + rawData;
        }
    }
}
