using InSummaryFunctions.Helpers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InSummaryFunctions
{
    /// <summary>
    /// Summarise new pdf blobs in a container, store summary as txt blob
    /// </summary>
    public static class TextSummarisationFunction
    {
        [FunctionName("TextSummarisation")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [BlobTrigger("summariseddocuments/{name}.{ext}")] Stream myBlob,
            [Blob("summariseddocuments/{name}.{ext}.summary.txt", FileAccess.Write)] CloudBlobStream summaryBlob,
            string name,
            string ext,
            TraceWriter log
            )
        {
            // Because suffix filters don't work yet - this should take non-pdfs off the todo list
            if (ext.ToLower() != "pdf")
                return;

            log.Info($"Text Processing beginning for {name} ({myBlob.Length} Bytes)");

            log.Info($"Extracting text from the PDF");
            var pages = iTextPDFHelper.GetPDFPages(myBlob, log, ocrImages: true);

            log.Info($"Calling Text Analytics to determine key phrases");
            Dictionary<string, int> keyPhrases = await TextAnalyticsHelper.GetKeyPhrases(pages, log);
            var topPhrases = keyPhrases.OrderByDescending(pair => pair.Value).Take(20).ToList();

            log.Info($"Building summary");
            string summary = TextAnalyticsHelper.BuildSummary(pages, topPhrases);

            log.Info($"Saving summary to new blob");
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(summary), false))
            {
                stream.CopyTo(summaryBlob);
            }
        }
    }
}