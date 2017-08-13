using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InSummaryFunctions.Helpers
{
    public static class AzureSearchHelper
    {
        private static SearchServiceClient _serviceClient = new SearchServiceClient(Constants.SearchServiceName, new SearchCredentials(Constants.SearchServiceAPIKey));
        private static ISearchIndexClient _indexClient = _serviceClient.Indexes.GetClient(Constants.IndexName);

        public static async Task UploadToAzureSearch(string pageId, string documentName, int pageNumber,  string keyPhrases, string text, TraceWriter log)
        {
            var document = new Document();
            document.Add("Id", pageId);
            document.Add("document_name", documentName);
            document.Add("page_number", pageNumber);
            document.Add("keyphrases", keyPhrases);
            document.Add("text", text);

            var indexOperations = new List<IndexAction>()
            {
                IndexAction.MergeOrUpload(document)
            };

            try
            {
                await _indexClient.Documents.IndexAsync(new IndexBatch(indexOperations));
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                log.Info("Failed to index some of the documents: " + string.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }
        }
    }
}
