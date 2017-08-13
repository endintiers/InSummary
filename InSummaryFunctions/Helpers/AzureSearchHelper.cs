using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace InSummaryFunctions.Helpers
{
    public static class AzureSearchHelper
    {
        private static SearchServiceClient _serviceClient = new SearchServiceClient(Constants.SearchServiceName, new SearchCredentials(Constants.SearchServiceAPIKey));

        public static async Task UploadToAzureSearch(string pageId, string documentName, int pageNumber,  string keyPhrases, string text, TraceWriter log)
        {
            // Create the index if it doesn't exist
            if (!_serviceClient.Indexes.Exists(Constants.IndexName))
            {
                var definition = new Index()
                {
                    Name = Constants.IndexName,
                    Fields = FieldBuilder.BuildForType<DocumentPage>()
                };
                _serviceClient.Indexes.Create(definition);
            }

            ISearchIndexClient indexClient = _serviceClient.Indexes.GetClient(Constants.IndexName);

            var documentPage = new DocumentPage
            {
                pageId = pageId,
                documentName = documentName,
                pageNumber = pageNumber,
                keyPhrases = keyPhrases,
                text = text
            };

            var actions = new IndexAction<DocumentPage>[]
            {
                IndexAction.MergeOrUpload(documentPage)
            };

            // Pretty small batch! Still fine for this simple demo
            var batch = IndexBatch.New(actions);

            try
            {
                await indexClient.Documents.IndexAsync(batch);
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

    [SerializePropertyNamesAsCamelCase]
    public partial class DocumentPage
    {
        [Key]
        public string pageId { get; set; }

        [IsFilterable]
        public string documentName { get; set; }

        public int pageNumber { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string keyPhrases { get; set; }

        [IsSearchable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string text { get; set; }

    }
}
