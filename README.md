# InSummary
Samples for "In Summary" talk
You will need:
- Visual Studio 2017 15.3+
- Visual Studio 2017 Tools for Azure Functions
- Cognitive Services

Optionally:
- An Azure Subscription
- An Azure Storage Instance (full not just blob)
- An Azure Search Instance

Get VS 2017 15.3 (Preview): https://www.visualstudio.com/vs/preview/
Built with Visual Studio 2017 Tools for Azure Functions - https://github.com/Azure/Azure-Functions
Uses the Webjobs SDK 2.1 (beta) - https://github.com/Azure/azure-webjobs-sdk
Free Cognitive Services Trial: https://azure.microsoft.com/en-us/try/cognitive-services/

No function.json needed (generated on publish) binding attributes replace this

Resource names and keys in Constants.cs need to be set for your resources

The starting point for this demo was Liam Cavanagh's project: https://github.com/liamca/AzureSearch-AzureFunctions-CognitiveServices 