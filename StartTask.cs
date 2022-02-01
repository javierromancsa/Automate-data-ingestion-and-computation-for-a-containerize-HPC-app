using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using System.Collections.Generic;

namespace BatchTask
{
    public static class StartTask
    {
        [FunctionName("StartTask")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // get body of request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            // deserialize as json            
            StartTaskParams startTaskParams = JsonConvert.DeserializeObject<StartTaskParams>(requestBody);
            // get config variables
            var baseurl = Environment.GetEnvironmentVariable("BaseUrl");
            var accountname = Environment.GetEnvironmentVariable("AccountName");
            var keyvalue = Environment.GetEnvironmentVariable("KeyValue");
            // get batch client            
            BatchClient batchClient = BatchClient.Open(new BatchSharedKeyCredentials(baseurl, accountname, keyvalue));
            // get pool
            //var pool = batchClient.PoolOperations.GetPool(poolname);
            var pool = batchClient.PoolOperations.GetPool(startTaskParams.poolName);
            // get job
            CloudJob job;
            try
            {
                //job = batchClient.JobOperations.GetJob(jobname);                
                job = batchClient.JobOperations.GetJob(startTaskParams.jobName);

            }
            catch(BatchException x)
            {
                job = batchClient.JobOperations.CreateJob(startTaskParams.jobName, new PoolInformation() { PoolId = startTaskParams.poolName });
                job.Commit();
            }
            
            // submit task
            string taskid = String.Format("{0}-{1}",startTaskParams.jobName, DateTime.Now.Ticks.ToString());
            string cmd = String.Format("{0}{1}",startTaskParams.taskCmd,startTaskParams.containerArgs);
            string image = Environment.GetEnvironmentVariable("ImageName");         
            CloudTask taskToAdd = new CloudTask(taskid, cmd);
            // if there is an image in config, let's use it
            if (!String.IsNullOrEmpty(image))
            {
                TaskContainerSettings cmdContainerSettings = new TaskContainerSettings(
                    imageName: image,              
                    containerRunOptions: "--rm"                    
                );
                List<OutputFile> ofiles = new List<OutputFile>();
                foreach(var of in startTaskParams.outputFiles)
                {
                    ofiles.Add(of.ToAzBatchOutputFile(taskid));
                }
                taskToAdd.ContainerSettings = cmdContainerSettings;
                taskToAdd.OutputFiles = ofiles;
                //taskToAdd.UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.NonAdmin));                

            }                        
            batchClient.JobOperations.AddTask(startTaskParams.jobName, taskToAdd);
            return new OkObjectResult(taskid + " submitted");
        }
    }
   
}
