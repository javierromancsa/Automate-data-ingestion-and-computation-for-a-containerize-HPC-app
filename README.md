# Automate data ingestion and event trigger HPC compute for containerize apps or tasks
Using ADF, Az Storage File Share, Az Blob, Az Function app, Az Batch and ACR, to Automate genomics raw sequence data for ingestion and computation.

![](../images/automation_diagram.png)


## BatchTask
This repo implements an Azure Function that runs on a Linux plan.
* Submits a batch job and task for existing pool.
* Uses a container for the task.
* Implemented in C#, .net core. 
* Sends output files to Blob
* Triggered by HTTP
* Expects a JSON body
* Task name will be the job name with a suffix of DateTime in ticks.

## Pre-requisites
* Azure Subscription
* Existing Azure Batch Account
* Existing Azure Container Registry with images to be used by tasks
* Existing Azure Batch Node Pool with configured images
* Viaual Studio 2019 or later version.  The code can also be ported to VS Code.

## Configuration Required (in the Function App)

| Name | Value |
| ---- | ----- |
| AccountName | Name of the Batch account to be used |
| BaseUrl | Full url of the Batch account, like https://yourbatchaccount.azregion.batch.azure.com |
| KeyValue | Batch Account Key.  To be used in order to create the job and submit the task |
| ImageName | Container Registry and image name to be used by the task.  Example: myregistry.azurecr.io/myimage:v1 |

## Sample JSON Body

```
{ 
  'poolName' : 'batchpool1',
  'jobName' : 'job1'
  'taskCmd' : '/software/myscript.sh'
  'containerArgs': 'arguments_for_the_task', 
  'outputFiles' : [
      {
            'destination' : {
                'container' : {
                    'containerUrl' : 'https://myblobaccount.blob.core.windows.net/myblobcontainer',
                    'path' : 'task_output',
                    'identityReference' : {
                        'resourceId' : '/subscriptions/mysubscription/resourceGroups/myRG/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mymanagedidentity'
                    }
                }
            },
            'filePattern' : '**/*',
            'uploadOptions' : {
                'uploadCondition' : 'taskcompletion'
            }
        },
        {
            'destination' : {
                'container' : {
                    'containerUrl' : 'https://myblobaccount.blob.core.windows.net/myblobcontainer',
                    'path' : 'task_output',
                    'identityReference' : {
                        'resourceId' : '/subscriptions/mysubscription/resourceGroups/myRG/providers/Microsoft.ManagedIdentity/userAssignedIdentities/mymanagedidentity'
                    }
                }
            },
            'filePattern' : '../std*.*',
            'uploadOptions' : {
                'uploadCondition' : 'taskcompletion'
            }
        }
    ] 
}

```

## Links
https://docs.microsoft.com/en-us/azure/batch/batch-docker-container-workloads

https://docs.microsoft.com/en-us/azure/batch/batch-task-output-files
