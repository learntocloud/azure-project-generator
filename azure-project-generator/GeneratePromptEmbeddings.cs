using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace azure_project_generator
{
    public class GeneratePromptEmbeddings
    {
        private readonly ILogger<GeneratePromptEmbeddings> _logger;

        public GeneratePromptEmbeddings(ILogger<GeneratePromptEmbeddings> logger)
        {
            _logger = logger;
        }

        [Function("GeneratePromptEmbeddings")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, String certificationCode)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // get certification json from cosmos db use cerificationCode as key to get correct item 

//            string prompt = $@"Generate a practical project idea for someone preparing for the {certificationCode} ({certificationName}) certification. The project should:

//1. Encompass multiple skills required for this certification, such as {skillName} and others.
//2. Utilize key Azure services relevant to the certification, including {serviceName} and related services.
//3. Address real-world scenarios that an Azure administrator might encounter.
//4. Include specific tasks that demonstrate proficiency in topics like {topicName}.
//5. Be scalable and follow Azure best practices.
//6. Be challenging enough to showcase advanced skills, but achievable for someone studying for the certification.
//7. Incorporate aspects of Azure governance and identity management where applicable.

//Provide a brief project description, main objectives, key Azure services to be used, and 2-3 specific tasks that would be part of implementing this project.";
            return new OkObjectResult($"The certificaton code is:  {certificationCode}");

        }
    }
}
