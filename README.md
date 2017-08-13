# InSummary
Samples for "In Summary" talk
You will need:
- Visual Studio 2017 15.3+
- Visual Studio 2017 Tools for Azure Functions
- Cognitive Services
- An Azure Subscription (but can do a few things without)
- An Azure Storage Instance (full not just blob) for TextSummarisation
- An Azure Search Instance for TextSearchIndexing

Get VS 2017 15.3 (Preview): https://www.visualstudio.com/vs/preview/

Built with Visual Studio 2017 Tools for Azure Functions - https://github.com/Azure/Azure-Functions

Uses the Webjobs SDK 2.1 (beta) - https://github.com/Azure/azure-webjobs-sdk

Free Cognitive Services Trial: https://azure.microsoft.com/en-us/try/cognitive-services/

Black Belt Connecting: https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-external-file

No function.json needed (generated on publish) binding attributes replace this

I'm using the one Azure Store for sample containers as well as logs etc. can be a different one.

Resource names and keys in Constants.cs and connections in local.settings.json need to be set for your resources. 

The starting point for this demo was Liam Cavanagh's project: https://github.com/liamca/AzureSearch-AzureFunctions-CognitiveServices 

