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
using System.Threading;
using Newtonsoft.Json;

namespace Org.Apache.REEF.Common.Telemetry
{
    /// <summary>
    /// Metrics of reference types (such as strings) should inherit from this class.
    /// </summary>
    /// <typeparam name="T">The type of the metric should be of reference type.</typeparam>
    public class MetricClass<T> : MetricBase<T> where T : class
    {

        public MetricClass(string name, string description, bool isImmutable = true)
            : base(name, description, isImmutable)
        {
        }

        [JsonConstructor]
        public MetricClass(string name, string description, T value)
            : base(name, description, value)
        {
        }

        public override void AssignNewValue(object val)
        {

            if (val.GetType() != _typedValue.GetType())
            {
                throw new ApplicationException("Cannot assign new value to metric because of type mismatch.");
            }
            Interlocked.Exchange(ref _typedValue, (T)val);
            _tracker.Track(val);
        }
    }
}
