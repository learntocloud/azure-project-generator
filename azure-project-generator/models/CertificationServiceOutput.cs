using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator.models
{
    public class CertificationServiceOutput
    {

        [CosmosDBOutput("%CosmosDb%", "%CertificationServiceOut%", Connection = "CosmosDBConnection")]
        public CertificationServiceDocument Document { get; set; }

        [BlobOutput("certservicearchive/{name}", Connection = "AzureWebJobsStorage")]
        public string ArchivedContent { get; set; }

    }
}
