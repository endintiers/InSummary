using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace InSummaryFunctions
{
    public static class OneDriveSummarisationFunction
    {
        // This SHOULD work but doesn't yet so not compiled. Waiting for more doc/betas
        [FunctionName("OneDriveSummarisation")]
        public static void Run(
            [ApiHubFileTrigger("OneDriveConnection", "{name}.{ext}")] Stream input,
            string name, string ext, TraceWriter log)
        {
            log.Info($"New File {name}.{ext}");
        }
    }
}