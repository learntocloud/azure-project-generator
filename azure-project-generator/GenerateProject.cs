using Azure;
using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;

namespace azure_project_generator
{
    public class GenerateProject
    {
        private readonly ILogger<GenerateProject> _logger;
        private readonly ContentGenerationService _contentGenerationService;

        public GenerateProject(ILogger<GenerateProject> logger, ContentGenerationService contentGenerationService)
        {
            _logger = logger;
            _contentGenerationService = contentGenerationService;
        }

        [Function("GenerateProject")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, string certificationCode,

             [CosmosDBInput(Connection = "CosmosDBConnection")] CosmosClient client)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var query = new QueryDefinition("SELECT * FROM c WHERE c.certificationCode = @certificationCode")
                     .WithParameter("@certificationCode", certificationCode);

            var iterator = client.GetContainer("AzureCertDB", "projectpromptvectors")
                                 .GetItemQueryIterator<CertificationProjectPromptDocument>(query);


            CertificationProjectPromptDocument certificationProjectPromptDocument = iterator.ReadNextAsync().Result.FirstOrDefault();

            float[] projectPromptVector = certificationProjectPromptDocument.ProjectPromptVector;

            var queryDef = new QueryDefinition
                (query: $"SELECT c.serviceName, c.skillName, c.topicName, VectorDistance(c.contextVector,@embedding) " +
                $"AS SimilarityScore FROM c ORDER BY VectorDistance(c.contextVector,@embedding)"
                ).WithParameter("@embedding", projectPromptVector);

            using FeedIterator<CertificationService> resultSetIterator =
                 client.GetContainer("AzureCertDB", "certvectors").GetItemQueryIterator<CertificationService>(queryDef);

            string projectServices = "";
            string projectSkills = "";

            while (resultSetIterator.HasMoreResults)
            {
                FeedResponse<CertificationService> feedResponse = resultSetIterator.ReadNextAsync().Result;
                foreach (var item in feedResponse)
                {
                    projectServices += item.ServiceName + " ";
                    projectSkills += item.SkillName + " ";

                }
            }

            string cloudProjectIdea = await _contentGenerationService.GenerateProjectIdeaAsync(projectSkills, projectServices);

            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(cloudProjectIdea);

            return response;
        }
    }
}
