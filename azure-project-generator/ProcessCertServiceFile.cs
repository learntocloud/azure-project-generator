using Azure;
using Azure.Storage.Blobs;
using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using System.Text.Json;



namespace azure_project_generator
{
    public class ProcessCertServiceFile
    {
        private readonly ILogger<ProcessCertServiceFile> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly JsonValidationService _jsonValidationService;
        private readonly ContentGenerationService _contentGenerationService;
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "certservice";
       

        public ProcessCertServiceFile(ILogger<ProcessCertServiceFile> logger,
            EmbeddingClient embeddingClient,
            JsonValidationService jsonValidationService,
            ContentGenerationService contentGenerationService, BlobServiceClient blobServiceClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
            _jsonValidationService = jsonValidationService ?? throw new ArgumentNullException(nameof(jsonValidationService));
            _contentGenerationService = contentGenerationService ?? throw new ArgumentNullException(nameof(contentGenerationService));
            _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        }

        [Function(nameof(ProcessCertServiceFile))]
        public async Task<CertificationServiceOutput> Run(
           [BlobTrigger($"{ContainerName}/{{name}}", Connection = "AzureWebJobsStorage")] string content,
           string name)
        {
            _logger.LogInformation($"Processing blob: {name}");
            CertificationServiceDocument certServiceDocument = new CertificationServiceDocument();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return new CertificationServiceOutput { Document = null };
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogError("Blob name is null or empty.");
                return new CertificationServiceOutput { Document = null };
            }

            if (!_jsonValidationService.ValidateJsonContent<CertificationService>(content))
            {
                _logger.LogError("Invalid JSON content.");
                return new CertificationServiceOutput { Document = null };
            }

            try
            {
                var mappedServiceData = JsonSerializer.Deserialize<CertificationService>(content);
            
                string contextSentence = _contentGenerationService.GenerateCertServiceContextSentence(mappedServiceData);
                if (string.IsNullOrEmpty(contextSentence))
                {
                    _logger.LogError("Context sentence generation failed.");
                    return new CertificationServiceOutput { Document = null };
                }
                float[] contentVector = await _contentGenerationService.GenerateEmbeddingsAsync(contextSentence).ConfigureAwait(false);


                certServiceDocument = CreateCertServiceDocument(mappedServiceData, contextSentence, contentVector);
                _logger.LogInformation("Document created successfully.");             

            }
            catch (Exception ex) when (ex is JsonException || ex is HttpRequestException || ex is RequestFailedException)
            {
                _logger.LogError(ex, $"An error occurred while processing blob {name}: {ex.Message}");
                return new CertificationServiceOutput { Document = null };
            }
            try
            {
                await _blobServiceClient.GetBlobContainerClient(ContainerName).GetBlobClient(name).DeleteIfExistsAsync();
                _logger.LogInformation($"Blob {name} deleted successfully.");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting blob {name}: {ex.Message}");
            }
            return new CertificationServiceOutput
            {
                Document = certServiceDocument,
            };
        }

        private CertificationServiceDocument CreateCertServiceDocument(CertificationService data, string contextSentence, float[] contentVector) =>
           new CertificationServiceDocument
           {
               Id = Guid.NewGuid().ToString(),
               CertificationServiceKey = $"{data.CertificationCode}-{data.ServiceName}",
               CertificationCode = data.CertificationCode,
               CertificationName = data.CertificationName,
               SkillName = data.SkillName,
               TopicName = data.TopicName,
               ServiceName = data.ServiceName,
               ContextSentence = contextSentence,
               ContextVector = contentVector
           };
    }
}
