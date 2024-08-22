using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace azure_project_generator
{
    public class ProcessFile
    {
        private readonly ILogger<ProcessFile> _logger;

        public ProcessFile(ILogger<ProcessFile> logger)
        {
            _logger = logger;
        }

        [Function(nameof(ProcessFile))]
        public async Task Run([BlobTrigger("raw/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");
        }
    }
}
