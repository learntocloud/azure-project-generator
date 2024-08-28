using Microsoft.Azure.Functions.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator
{
    public class MultipleOutput
    {

        [CosmosDBOutput("%CosmosDb%", "%CosmosContainerOut%", Connection = "CosmosDBConnection")]
        public CertServiceDocument CertServiceDocument { get; set; }

        [BlobOutput("certdataarchive/{name}", Connection = "AzureWebJobsStorage")]
        public string ArchivedContent { get; set; }

    }
}
