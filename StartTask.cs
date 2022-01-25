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
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            // get parameters
            string arguments = data?.containerArgs;
            string taskCmd = data?.taskCmd;
            string jobname = data?.jobName;
            string poolname = data?.poolName;            
            // get config variables
            var baseurl = Environment.GetEnvironmentVariable("BaseUrl");
            var accountname = Environment.GetEnvironmentVariable("AccountName");
            var keyvalue = Environment.GetEnvironmentVariable("KeyValue");
            // get batch client            
            BatchClient batchClient = BatchClient.Open(new BatchSharedKeyCredentials(baseurl, accountname, keyvalue));            
            // get pool
            var pool = batchClient.PoolOperations.GetPool(poolname);
            // get job
            CloudJob job;
            try
            {
                job = batchClient.JobOperations.GetJob(jobname);                
            }
            catch(BatchException x)
            {
                job = batchClient.JobOperations.CreateJob(jobname, new PoolInformation() { PoolId = poolname });
                job.Commit();
            }
            
            // submit task
            string taskid =  jobname + "-" + DateTime.Now.Ticks.ToString();
            string cmd = taskCmd + " " + arguments;
            string image = Environment.GetEnvironmentVariable("ImageName");         
            CloudTask taskToAdd = new CloudTask(taskid, cmd);
            // if there is an image in config, let's use it
            if (!String.IsNullOrEmpty(image))
            {
                TaskContainerSettings cmdContainerSettings = new TaskContainerSettings(
                    imageName: image,              
                    containerRunOptions: "--rm"                    
                );
                taskToAdd.ContainerSettings = cmdContainerSettings;
                taskToAdd.UserIdentity = new UserIdentity(new AutoUserSpecification(AutoUserScope.Task, ElevationLevel.NonAdmin));                

            }                        
            batchClient.JobOperations.AddTask(jobname, taskToAdd);
            return new OkObjectResult(taskid + " submitted");
        }
    }
   
}
