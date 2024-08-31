using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator.models
{
    public class CertificationProjectPromptOutput
    {
        [CosmosDBOutput("%CosmosDb%", "%CertificationPromptOut%", Connection = "CosmosDBConnection")]
        public CertificationProjectPromptDocument Document { get; set; }

        [BlobOutput("certdataarchive/{name}", Connection = "AzureWebJobsStorage")]
        public string ArchivedContent { get; set; }
    }
}
