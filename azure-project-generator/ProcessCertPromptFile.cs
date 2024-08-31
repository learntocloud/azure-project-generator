using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Embeddings;

namespace azure_project_generator
{
    public class ProcessCertPromptFile
    {
        private readonly ILogger<ProcessCertServiceFile> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly JsonValidationService _jsonValidationService;
        private readonly ContentGenerationService _contentGenerationService;

        public ProcessCertPromptFile(ILogger<ProcessCertServiceFile> logger,
            EmbeddingClient embeddingClient,
            JsonValidationService jsonValidationService,
            ContentGenerationService contentGenerationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
            _jsonValidationService = jsonValidationService ?? throw new ArgumentNullException(nameof(jsonValidationService));
            _contentGenerationService = contentGenerationService ?? throw new ArgumentNullException(nameof(contentGenerationService));
        }

        [Function(nameof(ProcessCertPromptFile))]
        public async Task<CertificationProjectPromptOutput> Run([BlobTrigger("certdata/{name}", Connection = "AzureWebJobsStorage")] string content, string name)
        {

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return new CertificationProjectPromptOutput { Document = null, ArchivedContent = null };
            }

            if (!_jsonValidationService.ValidateJsonContent<Certification>(content))
            {
                return new CertificationProjectPromptOutput { Document = null, ArchivedContent = null };
            }

            var certification = JsonConvert.DeserializeObject<Certification>(content);
            if (certification == null)
            {
                _logger.LogError("Failed to deserialize content to MappedService.");
                return new CertificationProjectPromptOutput { Document = null, ArchivedContent = null };
            }

            string contextSentence = _contentGenerationService.GenerateCertDataContextSentence(certification);
            float[] contentVector = await _contentGenerationService.GenerateEmbeddingsAsync(contextSentence);

            var certificationDocument = CreateCertificationProjectPromptDocument(certification, contextSentence, contentVector);

            _logger.LogInformation("Document created successfully.");
            _logger.LogInformation($"Archiving blob: {name}");


            return new CertificationProjectPromptOutput
            {
                Document = certificationDocument,
                ArchivedContent = content
            };
        }

        private CertificationProjectPromptDocument CreateCertificationProjectPromptDocument(Certification data, string contextSentence, float[] contentVector) =>
           new CertificationProjectPromptDocument
           {
               Id = Guid.NewGuid().ToString(),
               CertificationCode = data.CertificationCode,
               CertificationName = data.CertificationName,
               ProjectPrompt = contextSentence,
               ProjectPromptVector = contentVector
           };
    }
}

