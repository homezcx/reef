# Running HelloREEF on Azure Batch

## Prerequisites

You have [compiled REEF](https://cwiki.apache.org/confluence/display/REEF/Building+REEF) locally, and have [Azure Batch Pool configured](https://docs.microsoft.com/en-us/azure/batch/quick-create-portal#create-a-pool-of-compute-nodes). See [communication configuration instructions](#How-to-configure-REEF-.NET-Driver-Client-communication-on-Azure-Batch) to enable external batch communication. It is suggested to use data-science-vm published by microsoft-ads, which has Java pre-installed.

## Running HelloREEF on Azure Batch using Java

### How to configure REEF Java on Azure Batch

REEF Azure Batch runtime configuration is provided through a helper class ([AzureBatchRuntimeConfiguration.java](https://github.com/apache/reef/blob/master/lang/java/reef-runtime-azbatch/src/main/java/org/apache/reef/runtime/azbatch/client/AzureBatchRuntimeConfiguration.java)) which reads an avro configuration file.

The configuration can either set a system environment variable **REEF_AZBATCH_CONF** or a direct file path.

#### Load configuration through an environment variable:
```java
Configuration config = AzureBatchRuntimeConfiguration.fromEnvironment();
```

#### Load configuration through a file path:
```java
String pathName = "./dummyFilePath";
Configuration config = AzureBatchRuntimeConfiguration.fromTextFile(new File(pathName));
```

#### Sample configuration file:

```json
{
  "language": "Java",
  "Bindings": [
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.IsWindows",
      "value": "false"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountKey",
      "value": "dummyvalue1234562Wbg0CqnIdyFiZXr1G5URGnfRTVQnQ50LvB5+wnrr5ERS87TH/8K93ViZn/qfH0SGH4DKQ=="
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountName",
      "value": "reefbatchaccountname"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountUri",
      "value": "https://reefbatchaccountname.westus2.batch.azure.com"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchPoolId",
      "value": "myreefpool"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountName",
      "value": "reefstoragename"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountKey",
      "value": "dummyvalue123456Wh5+f8lN4H3BnwgIHi3Xj/ohNZt5sm8ZWK8jnKWWKD2r9WeBw8Yad5CGjyd7s9lSY01RDw=="
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageContainerName",
      "value": "reef-container"
    }
  ]
}
```

An example configuration can be seen in [HelloReefAzBatch.java](https://github.com/apache/reef/blob/master/lang/java/reef-examples/src/main/java/org/apache/reef/examples/hello/HelloReefAzBatch.java).

### How to launch HelloReefAzBatch

Running HelloReefAzBatch Java with no client:

```shell
java -cp lang/java/reef-examples/target/reef-examples-{$REEF_VERSION}-SNAPSHOT-shaded.jar org.apache.reef.examples.hello.HelloReefAzBatch
```

**Warning:** Due to a limitation of the current implementation, HelloReefAzBatch client is not supported unless the client is running on an Azure Batch node.

## Running HelloREEF on Azure Batch using .NET

**Warning:** Only Windows VMs are supported.

### How to configure REEF .NET on Azure Batch

Like running [REEF on Azure Batch using Java](#How-to-configure-REEF-Java-on-Azure-Batch), an example is provided in [HelloREEF.cs](https://github.com/homezcx/reef/blob/master/lang/cs/Org.Apache.REEF.Examples.HelloREEF/HelloREEF.cs).

### How to run REEF .NET on Azure Batch

Running HelloREEF .NET with client:

```shell
reef\lang\cs\bin\.netcore\Debug\Org.Apache.REEF.Examples.HelloREEF\net461>Org.Apache.REEF.Examples.HelloREEF.exe "azurebatch"
```

### How to configure REEF .NET Driver Client communication on Azure Batch

By default, an external entity cannot directly communicate with an Azure Batch node. In order to enable this communication, the Azure Batch Pool will need to have a configured [InboundNATPool](https://docs.microsoft.com/en-us/rest/api/batchservice/pool/add#inboundnatpool). 

The InboundNATPool maps individual frontend ports to individual batch nodes. For REEF usage, you will need to account for the number of nodes in the batch pool and the number of tasks expected to run on a node. The Frontend port range must span the same number of ports as there will be nodes. Likewise, there must be the same number of InboundEndPoints as tasks you expect to run on a node. Once configured, the list of possible backend ports should be specified in AzureBatchRuntimeClientConfiguration; like in [HelloREEF.cs](https://github.com/homezcx/reef/blob/master/lang/cs/Org.Apache.REEF.Examples.HelloREEF/HelloREEF.cs).

**Example InboundNATPool InboundEndPoints:**
| Name | Backend Port | Frontend port range | Protocol |
|-----------|:-----------:|:-----------:|:-----------:|
| Endpoint1 | 2000 |  1-100 |  tcp |
| Endpoint2 | 2001 |  101-200 |  tcp |

In Endpoint1, it maps each node's backend port (2000) to a frontend port number between 1 and 100. The client will then be able to talk to the backend port through the VM's public IP address and port, e.g. $(External IP):1 will map to $(Internal IP):2000. The user can retrieve a node's public IP address and frontend port through [Azure Batch ComputeNode InboundEndPoint](https://docs.microsoft.com/en-us/rest/api/batchservice/computenode/get#inboundendpoint).

In REEF, since Driver-Client communication relies on backend ports that are open to the public, the maxmium numbers of Driver tasks that can run on the same node, is the number of backend ports defined in InboundNATPool. This configuration has two InboundEndPoints (Endpoint1 and Endpoint2) and therefore only two drivers can run on one node. If more than two drivers try to run on a node, there won't be enough ports available for port binding. 

Likewise, this configuration has a frontend port range that spans 100 ports (1-100 and 101-200) and therefore only 100 nodes can properly use the port mappings. If more than 100 nodes are running tasks, they will run out of frontend ports for port mapping.

Assume a user's pool consists of 2 nodes with the following mapping established:
| Node Id | Endpoint| Public IP Address | Frontend port | Backend port |
|:-----------:|:-----------:|:-----------:|:-----------:|-----------|
| node1 | Endpoint1 | 13.0.0.20 | 1 | 2000 |
| node1 | Endpoint2 | 13.0.0.20 | 101 | 2001 |
| node2 | Endpoint1 | 13.0.0.20 | 2 | 2000 |
| node2 | Endpoint2 | 13.0.0.20 | 102 | 2001 |

To communicate to node1 on port 2001, the user will call through "13.0.0.20:101".

To communicate to node2 on port 2000, the user will call through "13.0.0.20.2".

#### Restrict the access when using InboundNATPool

User can use NetworkSecurityGroupRules to setup which IPs should be allowed to be able to talk to the port from outside; thus giving user ability to restrict who can contact the listener. An example can be found [here](https://docs.microsoft.com/en-us/azure/batch/pool-endpoint-configuration).
