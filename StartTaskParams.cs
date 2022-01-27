using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;

namespace BatchTask
{

    // Replicating Azure.Batch.OutputFile and members because the lack of setters makes it impossible to deserialize from Json :(
    public class OutputFileDestinationContainerIdentityReferenceParam
    {
        public string resourceId { get; set; }
    }

    public class OutputFileDestinationContainerParam
    {
        public string containerUrl { get; set; }
        public string path { get; set; }
        public OutputFileDestinationContainerIdentityReferenceParam identityReference { get; set; }
    }
    public class OutputFileDestinationParam
    {
        public OutputFileDestinationContainerParam container { get; set; }
    }

    public class OutputFileUploadOptionsParam
    {
        public string uploadCondition { get; set; }
    }

    public class OutputFileParam
    {
        public OutputFileDestinationParam destination { get; set; }
        public string filePattern { get; set; }
        public OutputFileUploadOptionsParam uploadOptions { get; set; }

        public Microsoft.Azure.Batch.OutputFile ToAzBatchOutputFile()
        {
            ComputeNodeIdentityReference identityReference = new ComputeNodeIdentityReference() { ResourceId = this.destination.container.identityReference.resourceId };
            OutputFileBlobContainerDestination container = new OutputFileBlobContainerDestination(this.destination.container.containerUrl, identityReference, this.destination.container.path);
            OutputFileDestination destination = new OutputFileDestination(container);
            string cond = this.uploadOptions.uploadCondition.ToLower();
            OutputFileUploadCondition uploadCondition = cond.Equals("taskcomplation") ? OutputFileUploadCondition.TaskCompletion : (cond.Equals("taskfailure") ? OutputFileUploadCondition.TaskFailure : OutputFileUploadCondition.TaskSuccess);
            OutputFileUploadOptions uploadOptions = new OutputFileUploadOptions(uploadCondition);
            return new Microsoft.Azure.Batch.OutputFile(this.filePattern, destination, uploadOptions);                
        }
    }

    public class StartTaskParams
    {
        [JsonProperty("poolName")]
        public string poolName { get; set; }

        [JsonProperty("jobName")]
        public string jobName { get; set; }

        [JsonProperty("taskCmd")]
        public string taskCmd { get; set; }

        [JsonProperty("containerArgs")]
        public string containerArgs { get; set; }

        [JsonProperty("outputFiles")]
        public List<OutputFileParam> outputFiles { get; set; }
    }
}
