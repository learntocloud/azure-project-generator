using Azure;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get configuration
        var config = context.Configuration;

        // Initialize Azure OpenAI client
        string keyFromEnvironment = config["AZURE_OPENAI_API_KEY"];
        string endpointFromEnvironment = config["AZURE_OPENAI_API_ENDPOINT"];
        string embeddingsDeployment = config["EMBEDDINGS_DEPLOYMENT"];
        string azureWebJobsStorage = config["AzureWebJobsStorage"];
        string completionsDeployment = config["COMPLETIONS_DEPLOYMENT"];

        if (string.IsNullOrEmpty(keyFromEnvironment) || string.IsNullOrEmpty(endpointFromEnvironment) || string.IsNullOrEmpty(embeddingsDeployment))
        {
            throw new InvalidOperationException("Required Azure OpenAI configuration is missing.");
        }

        // Register BlobServiceClient as a singleton
        services.AddSingleton(new BlobServiceClient(azureWebJobsStorage));

        AzureOpenAIClient azureClient = new(
            new Uri(endpointFromEnvironment),
            new AzureKeyCredential(keyFromEnvironment));

        // Register EmbeddingClient as a singleton
        services.AddSingleton(azureClient.GetEmbeddingClient(embeddingsDeployment));

        // Register ChatClient as a singleton
        services.AddSingleton(azureClient.GetChatClient(completionsDeployment));

        // Register JsonValidationService
        services.AddSingleton<JsonValidationService>();

        // Register ContentGenerationService
        services.AddSingleton<ContentGenerationService>();
    })
    .Build();

host.Run();
