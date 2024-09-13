using Azure;
using Azure.Storage.Blobs;
using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Embeddings;



namespace azure_project_generator
{
    public class ProcessCertServiceFile
    {
        private readonly ILogger<ProcessCertServiceFile> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly JsonValidationService _jsonValidationService;
        private readonly ContentGenerationService _contentGenerationService;
        private readonly BlobServiceClient _blobServiceClient;

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
           [BlobTrigger("certservice/{name}", Connection = "AzureWebJobsStorage")] string content,
           string name)
        {
            _logger.LogInformation($"Processing blob: {name}");

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return new CertificationServiceOutput { Document = null };
            }

            if (!_jsonValidationService.ValidateJsonContent<CertificationService>(content))
            {
                _logger.LogError("Invalid JSON content.");
                return new CertificationServiceOutput { Document = null };
            }

            try
            {
                var mappedServiceData = JsonConvert.DeserializeObject<CertificationService>(content);
                if (mappedServiceData == null)
                {
                    _logger.LogError("Failed to deserialize content to CertificationService.");
                    return new CertificationServiceOutput { Document = null };
                }

                string contextSentence = _contentGenerationService.GenerateCertServiceContextSentence(mappedServiceData);
                float[] contentVector = await _contentGenerationService.GenerateEmbeddingsAsync(contextSentence);

                var certServiceDocument = CreateCertServiceDocument(mappedServiceData, contextSentence, contentVector);

                _logger.LogInformation("Document created successfully.");
                _logger.LogInformation($"Deleting blob: {name}");

                // delete the blob
                BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient("certservice");
                BlobClient blobClient = blobContainerClient.GetBlobClient(name);
                await blobClient.DeleteAsync();

                return new CertificationServiceOutput
                {
                    Document = certServiceDocument,
                };
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON error occurred: {jsonEx.Message}");
                return new CertificationServiceOutput { Document = null };
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP request error occurred: {httpEx.Message}");
                return new CertificationServiceOutput { Document = null };
            }
            catch (RequestFailedException storageEx)
            {
                _logger.LogError($"Azure Storage error occurred: {storageEx.Message}");
                return new CertificationServiceOutput { Document = null };
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
                return new CertificationServiceOutput { Document = null };
            }
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
