using azure_project_generator.models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using System.ComponentModel;

namespace azure_project_generator
{
    public class GenerateProject
    {
        private readonly ILogger<GenerateProject> _logger;

        public GenerateProject(ILogger<GenerateProject> logger)
        {
            _logger = logger;
        }

        [Function("GenerateProject")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, string certificationCode,

             [CosmosDBInput(Connection = "CosmosDBConnection")] CosmosClient client)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var query = new QueryDefinition("SELECT * FROM c WHERE c.certificationCode = @certificationCode")
                     .WithParameter("@certificationCode", certificationCode);

            var iterator = client.GetContainer("AzureCertDB", "projectpromptvectors")
                                 .GetItemQueryIterator<CertificationProjectPromptDocument>(query);


            CertificationProjectPromptDocument certificationProjectPromptDocument = iterator.ReadNextAsync().Result.FirstOrDefault();

            float[] projectPromptVector = certificationProjectPromptDocument.ProjectPromptVector;
            //"SELECT * FROM c WHERE c.certificationCode = {certificationCode}"


            var queryDef = new QueryDefinition(
      query: $"SELECT c.serviceName, VectorDistance(c.contextVector,@embedding) AS SimilarityScore FROM c ORDER BY VectorDistance(c.contextVector,@embedding)"
      ).WithParameter("@embedding", projectPromptVector);

            using FeedIterator<CertificationServiceDocument> resultSetIterator = client.GetContainer("AzureCertDB", "certvectors").GetItemQueryIterator<CertificationServiceDocument>(queryDef);
            
            while (resultSetIterator.HasMoreResults)
            {
                FeedResponse<CertificationServiceDocument> response = resultSetIterator.ReadNextAsync().Result;
                foreach (var item in response)
                {
                    _logger.LogInformation(item.ServiceName);
                }
            }

            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
