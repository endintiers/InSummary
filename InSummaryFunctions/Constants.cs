using System;
using System.Text.RegularExpressions;

namespace InSummaryFunctions
{
    static class Constants
    {
        public const string SearchServiceName = "insummary";
        public const string SearchServiceAPIKey = "XXXXXXXXXXXXXXXXXX";
        public const string IndexName = "document-idx";

        public const string VisionAPIKey = "XXXXXXXXXXXXX";
        public const string TextAnalyticsAPIKey = "XXXXXXXXXXXXXXXXX";
        public const string CognitiveServicesBaseUrl = "https://westus.api.cognitive.microsoft.com";
        public static Uri TextAnalyticsKeyPhraseUri = new Uri(CognitiveServicesBaseUrl + "/text/analytics/v2.0/keyPhrases");

        public const int MaxSentencesInASummary = 10;
        public static Regex FindSentencesRegex = new Regex(@"(?<=[\.!\?])\s+", RegexOptions.Compiled | RegexOptions.Multiline);
    }
}
