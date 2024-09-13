using Microsoft.Azure.Functions.Worker;

namespace azure_project_generator.models
{
    public class CertificationServiceOutput
    {

        [CosmosDBOutput("%CosmosDb%", "%CertificationServiceOut%", Connection = "CosmosDBConnection")]
        public CertificationServiceDocument Document { get; set; }

    }
}
