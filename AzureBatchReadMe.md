# Running HelloREEF on Azure Batch

## Prerequisites

[You have compiled REEF locally](https://cwiki.apache.org/confluence/display/REEF/Building+REEF), and have [Azure Batch Pool](https://docs.microsoft.com/en-us/azure/batch/quick-create-portal#create-a-pool-of-compute-nodes) configured. It is suggested to use data-science-vm published by microsoft-ads, which has Java pre-installed.

## Running HelloREEF on Azure Batch using Java

### How to configure REEF Java on Azure Batch

REEF Azure Batch runtime configuration is provided through a helper class [AzureBatchRuntimeConfiguration](https://github.com/apache/reef/blob/master/lang/java/reef-runtime-azbatch/src/main/java/org/apache/reef/runtime/azbatch/client/AzureBatchRuntimeConfiguration.java). User can either provide an environment variable **REEF_AZBATCH_CONF** to an configuration file path, or provide its File class instance. A sample configuration file is:

```json
{
  "language": "Java",
  "Bindings": [
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.IsWindows",
      "value": "true"
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountKey",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountName",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchAccountUri",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureBatchPoolId",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountName",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageAccountKey",
      "value": ""
    },
    {
      "key": "org.apache.reef.runtime.azbatch.parameters.AzureStorageContainerName",
      "value": ""
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
reef\lang\cs\bin\.netcore\Debug\Org.Apache.REEF.Examples.HelloREEF\net461>Org.Apache.REEF.Examples.HelloREEF.exe
```

### How to configure REEF .NET Driver Client communication on Azure Batch

The communication is enabled by Azure Batch feature [InboundNATPool](https://docs.microsoft.com/en-us/rest/api/batchservice/pool/add#inboundnatpool). User will need to define his InboundNATPool rules when setting up Azure Batch Pool. In addition, a list of possible ports user intends to use should be specified in AzureBatchRuntimeClientConfiguration, like in [HelloREEF.cs](https://github.com/homezcx/reef/blob/master/lang/cs/Org.Apache.REEF.Examples.HelloREEF/HelloREEF.cs).