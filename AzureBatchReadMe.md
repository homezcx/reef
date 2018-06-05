# Running HelloREEF on Azure Batch

## Prerequisites

[You have compiled REEF locally](https://cwiki.apache.org/confluence/display/REEF/Building+REEF), and have [Azure Batch Pool](https://docs.microsoft.com/en-us/azure/batch/quick-create-portal#create-a-pool-of-compute-nodes) configured. It is suggested to use data-science-vm published by microsoft-ads, which has Java pre-installed.

## Running HelloREEF on Azure Batch using Java

### How to configure REEF Java on Azure Batch

REEF Azure Batch runtime configuration is provided through a helper class [AzureBatchRuntimeConfiguration](https://github.com/apache/reef/blob/master/lang/java/reef-runtime-azbatch/src/main/java/org/apache/reef/runtime/azbatch/client/AzureBatchRuntimeConfiguration.java) reads avro configuration file.

User can either set an system environment variable **REEF_AZBATCH_CONF** to an configuration file path, then call

```java
Configuration config = AzureBatchRuntimeConfiguration.fromEnvironment();
```

or provide its java.io.File class instance

```java
String pathName = "./dummyFilePath";
Configuration config = AzureBatchRuntimeConfiguration.fromTextFile(new File(pathName));
```

A sample configuration file is:

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
      "value": "LinuxPool"
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

An example of configuration is [HelloReefAzBatch.java](https://github.com/apache/reef/blob/master/lang/java/reef-examples/src/main/java/org/apache/reef/examples/hello/HelloReefAzBatch.java).

### How to launch HelloReefAzBatch

Running HelloReefAzBatch Java with no client:

```shell
java -cp lang/java/reef-examples/target/reef-examples-{$REEF_VERSION}-SNAPSHOT-shaded.jar org.apache.reef.examples.hello.HelloReefAzBatch
```

Due to current limitation of implentation, HelloReefAzBatch client is not supported unless client is running in one of Azure Batch node.

## Running HelloREEF on Azure Batch using .NET

### NOTE

Only windows VM is supported.

### How to configure REEF .NET on Azure Batch

Like running in REEF. Java on Azure Batch, an example is provided in [HelloREEF.cs](https://github.com/homezcx/reef/blob/master/lang/cs/Org.Apache.REEF.Examples.HelloREEF/HelloREEF.cs).

### How to run REEF .NET on Azure Batch

Running HelloREEF .NET with client:

```shell
reef\lang\cs\bin\.netcore\Debug\Org.Apache.REEF.Examples.HelloREEF\net461>Org.Apache.REEF.Examples.HelloREEF.exe "azurebatch"
```

### How to configure REEF .NET Driver Client communication on Azure Batch

The communication is enabled by Azure Batch feature [InboundNATPool](https://docs.microsoft.com/en-us/rest/api/batchservice/pool/add#inboundnatpool). User will need to define his InboundNATPool endpoints when setting up Azure Batch Pool. In addition, a list of possible ports user intends to use, among the backend ports, should be specified in AzureBatchRuntimeClientConfiguration, like in [HelloREEF.cs](https://github.com/homezcx/reef/blob/master/lang/cs/Org.Apache.REEF.Examples.HelloREEF/HelloREEF.cs).

An InboundNATPool can define several InboundEndPoints:

| Name | Backend Port | Frontend port range | Protocol |
|-----------|:-----------:|:-----------:|:-----------:|
| Endpoint1 | 2000 | 1-100 | tcp |
| Endpoint2 | 2001 | 101-200 | tcp |

In Endpoint1, it maps each VM's backend port 2000, to a frontend port number. In this case, frontend port range should be larger or equal to the number of VMs in the pool. User will be able to talk to the backend port through VM public IP and port. User can retrieve a node's public IP and frontend port through [Azure Batch ComputeNode InboundEndPoint](https://docs.microsoft.com/en-us/rest/api/batchservice/computenode/get#inboundendpoint).

In REEF, since Driver-Client communication relies on backend ports that opens to public, the maxmium numbers of Driver task to be allowed running on the same node, is the number of backend ports defined in InboundNATPool.

Assume user's pool consists of 2 nodes, he will have such mapping established:

| Node Id and Endpoint| Public IP Address | Frontend port |
|:-----------:|:-----------:|:-----------:|
| node1 Endpoint1 | 13.66.208.20| 1 |
| node1 Endpoint2 | 13.66.208.20| 101 |
| node2 Endpoint1 | 13.66.208.20| 2 |
| node2 Endpoint2 | 13.66.208.20| 102 |

To communicate to node1, port 2001, user will call through "13.66.208.20:101";
To communicate to node2, port 2000, user will call through "13.66.208.20.2".

#### Restrict the access when using InboundNATPool

User can use NetworkSecurityGroupRules to setup which IPs should be allowed to be able to talk to the port from outside. Thus giving user ability to restrict who can contact the listener. An example is [here](https://docs.microsoft.com/en-us/azure/batch/pool-endpoint-configuration).
