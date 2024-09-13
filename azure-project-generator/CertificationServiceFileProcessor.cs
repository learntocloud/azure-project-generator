using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Embeddings;

namespace azure_project_generator
{
    public class CertificationServiceFileProcessor
    {
        private readonly ILogger<CertificationServiceFileProcessor> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly JsonValidationService _jsonValidationService;
        private readonly ContentGenerationService _contentGenerationService;

        public CertificationServiceFileProcessor(ILogger<CertificationServiceFileProcessor> logger,
            EmbeddingClient embeddingClient,
            JsonValidationService jsonValidationService,
            ContentGenerationService contentGenerationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
            _jsonValidationService = jsonValidationService ?? throw new ArgumentNullException(nameof(jsonValidationService));
            _contentGenerationService = contentGenerationService ?? throw new ArgumentNullException(nameof(contentGenerationService));
        }

        [Function(nameof(CertificationServiceFileProcessor))]
        public async Task<CertificationServiceOutput> Run(
           [BlobTrigger("certservice/{name}", Connection = "AzureWebJobsStorage")] string content,
           string name)
        {
            _logger.LogInformation($"Processing blob: {name}");

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return new CertificationServiceOutput { Document = null};
            }

            if (!_jsonValidationService.ValidateJsonContent<CertificationService>(content))
            {
                return new CertificationServiceOutput { Document = null };
            }

            var mappedServiceData = JsonConvert.DeserializeObject<CertificationService>(content);
            if (mappedServiceData == null)
            {
                _logger.LogError("Failed to deserialize content to MappedService.");
                return new CertificationServiceOutput { Document = null };
            }

            string contextSentence = _contentGenerationService.GenerateCertServiceContextSentence(mappedServiceData);
            float[] contentVector = await _contentGenerationService.GenerateEmbeddingsAsync(contextSentence);

            var certServiceDocument = CreateCertServiceDocument(mappedServiceData, contextSentence, contentVector);

            _logger.LogInformation("Document created successfully.");
            _logger.LogInformation($"Archiving blob: {name}");


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
