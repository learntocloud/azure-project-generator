using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace azure_project_generator
{
    public class GenerateProjectFromConcept
    {
        private readonly ILogger<GenerateProjectFromConcept> _logger;
        private ContentGenerationService _contentGenerationService;

        public GenerateProjectFromConcept(ILogger<GenerateProjectFromConcept> logger, ContentGenerationService contentGenerationService)
        {
            _logger = logger;
            _contentGenerationService = contentGenerationService;
        }

        [Function("GenerateProjectFromConcept")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Admin, "get", "post")] HttpRequestData req, string concept,
             [CosmosDBInput(Connection = "CosmosDBConnection")]
        CosmosClient client)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string projectPrompt = $"I need a project idea for the cloud engineering concept exam {concept}";

            float[] projectPromptVector = _contentGenerationService.GenerateEmbeddingsAsync(projectPrompt).Result;

            var queryDef = new QueryDefinition(
    query: $"SELECT TOP 5 c.serviceName, c.skillName, c.topicName, VectorDistance(c.contextVector, @embedding) " +
           $"AS SimilarityScore FROM c ORDER BY VectorDistance(c.contextVector, @embedding)"
    ).WithParameter("@embedding", projectPromptVector);

            using FeedIterator<CertificationService> resultSetIterator =
                 client.GetContainer("AzureCertDB", "certvectors").GetItemQueryIterator<CertificationService>(queryDef);

            List<string> projectServices = new List<string>();

            while (resultSetIterator.HasMoreResults)
            {
                FeedResponse<CertificationService> feedResponse = resultSetIterator.ReadNextAsync().Result;
                foreach (var item in feedResponse)
                {
                    projectServices.Add(item.ServiceName);
                }
            }
            string cloudProjectIdea = await _contentGenerationService.GenerateProjectIdeaFromConceptAsync(projectServices, concept);


            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(cloudProjectIdea);

            return response;
        }
    }
}
