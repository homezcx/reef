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

//<auto-generated />

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract
{
    /// <summary>
    /// Used to serialize and deserialize Avro record org.apache.reef.tang.implementation.avro.AvroClassNode.
    /// </summary>
    [DataContract(Namespace = "org.apache.reef.tang.implementation.avro")]
    [KnownType(typeof(List<Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract.AvroConstructorDef>))]
    [KnownType(typeof(List<string>))]
    public partial class AvroClassNode
    {
        private const string JsonSchema = @"{""type"":""record"",""name"":""org.apache.reef.tang.implementation.avro.AvroClassNode"",""fields"":[{""name"":""isInjectionCandidate"",""type"":""boolean""},{""name"":""isExternalConstructor"",""type"":""boolean""},{""name"":""isUnit"",""type"":""boolean""},{""name"":""injectableConstructors"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""org.apache.reef.tang.implementation.avro.AvroConstructorDef"",""fields"":[{""name"":""fullClassName"",""type"":""string""},{""name"":""constructorArgs"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""org.apache.reef.tang.implementation.avro.AvroConstructorArg"",""fields"":[{""name"":""fullArgClassName"",""type"":""string""},{""name"":""namedParameterName"",""type"":[""null"",""string""]},{""name"":""isInjectionFuture"",""type"":""boolean""}]}}}]}}},{""name"":""otherConstructors"",""type"":{""type"":""array"",""items"":""org.apache.reef.tang.implementation.avro.AvroConstructorDef""}},{""name"":""implFullNames"",""type"":{""type"":""array"",""items"":""string""}},{""name"":""defaultImplementation"",""type"":[""null"",""string""]}]}";

        /// <summary>
        /// Gets the schema.
        /// </summary>
        public static string Schema
        {
            get
            {
                return JsonSchema;
            }
        }
      
        /// <summary>
        /// Gets or sets the isInjectionCandidate field.
        /// </summary>
        [DataMember]
        public bool isInjectionCandidate { get; set; }
              
        /// <summary>
        /// Gets or sets the isExternalConstructor field.
        /// </summary>
        [DataMember]
        public bool isExternalConstructor { get; set; }
              
        /// <summary>
        /// Gets or sets the isUnit field.
        /// </summary>
        [DataMember]
        public bool isUnit { get; set; }
              
        /// <summary>
        /// Gets or sets the injectableConstructors field.
        /// </summary>
        [DataMember]
        public List<Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract.AvroConstructorDef> injectableConstructors { get; set; }
              
        /// <summary>
        /// Gets or sets the otherConstructors field.
        /// </summary>
        [DataMember]
        public List<Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract.AvroConstructorDef> otherConstructors { get; set; }
              
        /// <summary>
        /// Gets or sets the implFullNames field.
        /// </summary>
        [DataMember]
        public List<string> implFullNames { get; set; }
              
        /// <summary>
        /// Gets or sets the defaultImplementation field.
        /// </summary>
        [DataMember]
        public string defaultImplementation { get; set; }
                
        /// <summary>
        /// Initializes a new instance of the <see cref="AvroClassNode"/> class.
        /// </summary>
        public AvroClassNode()
        {
            this.defaultImplementation = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvroClassNode"/> class.
        /// </summary>
        /// <param name="isInjectionCandidate">The isInjectionCandidate.</param>
        /// <param name="isExternalConstructor">The isExternalConstructor.</param>
        /// <param name="isUnit">The isUnit.</param>
        /// <param name="injectableConstructors">The injectableConstructors.</param>
        /// <param name="otherConstructors">The otherConstructors.</param>
        /// <param name="implFullNames">The implFullNames.</param>
        /// <param name="defaultImplementation">The defaultImplementation.</param>
        public AvroClassNode(bool isInjectionCandidate, bool isExternalConstructor, bool isUnit, List<Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract.AvroConstructorDef> injectableConstructors, List<Org.Apache.REEF.Tang.Implementations.ClassHierarchy.AvroDataContract.AvroConstructorDef> otherConstructors, List<string> implFullNames, string defaultImplementation)
        {
            this.isInjectionCandidate = isInjectionCandidate;
            this.isExternalConstructor = isExternalConstructor;
            this.isUnit = isUnit;
            this.injectableConstructors = injectableConstructors;
            this.otherConstructors = otherConstructors;
            this.implFullNames = implFullNames;
            this.defaultImplementation = defaultImplementation;
        }
    }
}