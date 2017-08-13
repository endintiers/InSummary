using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.Search;
using System.Web;
using InSummaryFunctions.Helpers;

namespace InSummaryFunctions
{
    /// <summary>
    /// Extract and/or OCR text from pdf blobs in a container, get key phrases and index for Azure Search
    /// </summary>
    public static class TextSearchIndexingFunction
    {
        [FunctionName("TextSearchIndexing")]
        [StorageAccount("AzureWebJobsStorage")]
        public static async Task Run(
            [BlobTrigger("searchabledocuments/{name}.{ext}", Connection = "AzureWebJobsStorage")]Stream myBlob,
            string name, string ext, TraceWriter log)
        {
            // Because suffix filters don't work yet - this should take non-pdfs off the todo list
            if (ext.ToLower() != "pdf")
                return;

            log.Info($"Text Processing beginning for {name} ({myBlob.Length} Bytes)");

            log.Info($"Extracting text from the PDF (including OCR");
            var pages = iTextPDFHelper.GetPDFPages(myBlob, log, ocrImages: true);

            log.Info($"Calling Text Analytics to determine key phrases");
            Dictionary<string, int> keyPhrases = await TextAnalyticsHelper.GetKeyPhrases(pages, log);

            log.Info($"Uploading document to Azure Search");
            foreach (var page in pages)
            {
                string pageId = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(name + "." + ext + page.Number));
                await AzureSearchHelper.UploadToAzureSearch(pageId, name + "." + ext, page.Number, page.KeyPhrases, page.Text, log);
            }
        }
    }
}