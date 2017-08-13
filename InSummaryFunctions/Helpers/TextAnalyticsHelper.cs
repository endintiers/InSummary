using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InSummaryFunctions.Helpers
{
    public class TextAnalyticsHelper
    {
        //public static async Task<Dictionary<string, int>> GetKeyPhrases(List<PDFPage> pages, TraceWriter log)
        //{
        //    var documents = new List<dynamic>();
        //    foreach (var page in pages)
        //    {
        //        documents.Add(new { id = page.Number, text = page.Text});
        //    }
        //    Dictionary<string, int> keyPhrases = await GetKeyPhrases(documents, log);
        //    return keyPhrases;
        //}

        // documents List has to be id and text
        public static async Task<Dictionary<string, int>> GetKeyPhrases(List<PDFPage> pages, TraceWriter log)
        {
            var pageDict = new Dictionary<int, PDFPage>();
            var documents = new List<dynamic>();
            foreach (var page in pages)
            {
                pageDict.Add(page.Number, page);
                documents.Add(new { id = page.Number, text = page.Text });
            }

            Dictionary<string, int> keyPhrases = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Constants.TextAnalyticsAPIKey);

            string requestPayload = JsonConvert.SerializeObject(new { documents = documents });
            var request = new HttpRequestMessage(HttpMethod.Post, Constants.TextAnalyticsKeyPhraseUri)
            {
                Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = response.Content?.ReadAsStringAsync().Result;
                log.Error("Request failed: " + error);
                return keyPhrases;
            }

            string content = await response.Content.ReadAsStringAsync();
            var jsresponse = JsonConvert.DeserializeObject<KeyPhrasesResponse>(content);

            //dynamic responsePayload = JsonConvert.DeserializeObject<dynamic>(content);
            //foreach (dynamic document in responsePayload.documents)
            foreach(var document in jsresponse.documents)
            {
                var pageNum = 1;
                int.TryParse(document.id, out pageNum);
                pageDict[pageNum].KeyPhrases = string.Join(", ",document.keyPhrases);

                foreach (string keyPhrase in document.keyPhrases)
                {
                    int count;
                    if (keyPhrases.TryGetValue(keyPhrase.ToLower(), out count))
                    {
                        count++;
                        keyPhrases[keyPhrase.ToLower()] = count;
                    }
                    else
                    {
                        keyPhrases[keyPhrase.ToLower()] = 1;
                    }
                }
            }
            return keyPhrases;
        }

        public static string BuildSummary(List<PDFPage> pages, List<KeyValuePair<string, int>> phrases)
        {
            List<string> sentences = new List<string>();
            foreach(var page in pages)
            {
                sentences.AddRange(Constants.FindSentencesRegex.Split(page.Text));
            }

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("Phrases: " + String.Join(", ", phrases.Select(p => p.Key.ToString().ToLower())));

            // Cant be bothered using LINQ here - I like the bleeding obvious anyhow...
            List<Tuple<string, int>> sentenceCount = new List<Tuple<string, int>>();
            foreach (var sentence in sentences)
            {
                var phraseCount = 0;
                foreach (var phrase in phrases)
                {
                    if (sentence.ToLower().Contains(phrase.Key.ToLower()))
                        phraseCount++;
                }
                sentenceCount.Add(new Tuple<string, int>(sentence, phraseCount));
            }

            var importantSentences = sentenceCount.OrderByDescending(s => s.Item2).Take(Constants.MaxSentencesInASummary);
            List<string> selectedSentences = importantSentences.Select(s => Regex.Replace(s.Item1, @"\r\n?|\n", " ", RegexOptions.Compiled)).ToList<string>();
            foreach (var selectedSentence in selectedSentences)
            {
                summary.AppendLine(selectedSentence);
            }
            return summary.ToString();
        }
    }

    public class KeyPhrasesResponse
    {
        public class Document
        {
            public List<string> keyPhrases { get; set; }
            public string id { get; set; }
        }
        public class Error
        {
            public string id { get; set; }
            public string message { get; set; }
        }
        public List<Document> documents { get; set; }
        public List<Error> errors { get; set; }
    }
}
