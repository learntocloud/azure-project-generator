using Azure.Storage.Blobs;
using azure_project_generator.models;
using azure_project_generator.services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Embeddings;
using System.Text;

namespace azure_project_generator
{
    public class CertificationDataFileProcessor
    {
        private readonly ILogger<CertificationServiceFileProcessor> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly JsonValidationService _jsonValidationService;
        private readonly ContentGenerationService _contentGenerationService;
        private readonly BlobServiceClient _blobServiceClient;

        public CertificationDataFileProcessor(ILogger<CertificationServiceFileProcessor> logger,
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

        [Function(nameof(CertificationDataFileProcessor))]
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

            // create a json file for each service in the certification

            foreach (var skill in certification.SkillsMeasured)
            {
                foreach (var topic in skill.Topics)
                {
                    foreach (var service in topic.Services)
                    {
                        var serviceData = new CertificationService
                        {
                            CertificationCode = certification.CertificationCode,
                            CertificationName = certification.CertificationName,
                            SkillName = skill.Name,
                            TopicName = topic.TopicName,
                            ServiceName = service
                        };
                        BlobContainerClient blobContainerClient = _blobServiceClient.GetBlobContainerClient("certservice");
                        var serviceJson = JsonConvert.SerializeObject(serviceData);
                        var serviceBlobName = $"{certification.CertificationCode}-{serviceData.ServiceName}.json";
                        
                        BlobClient blobClient = blobContainerClient.GetBlobClient(serviceBlobName);
                        await blobClient.UploadAsync(new MemoryStream(Encoding.UTF8.GetBytes(serviceJson)), true);
                    }
                }
            }

            string contextSentence = _contentGenerationService.GenerateCertDataContextSentence(certification);

            float[] cerificationCodeVector = await _contentGenerationService.GenerateEmbeddingsAsync(contextSentence);

            var certificationDocument = CreateCertificationProjectPromptDocument(certification, cerificationCodeVector);

            _logger.LogInformation("Document created successfully.");
            _logger.LogInformation($"Archiving blob: {name}");


            return new CertificationProjectPromptOutput
            {
                Document = certificationDocument,
                ArchivedContent = content
            };
        }

        private CertificationProjectPromptDocument CreateCertificationProjectPromptDocument(Certification data, float[] contentVector) =>
           new CertificationProjectPromptDocument
           {
               Id = Guid.NewGuid().ToString(),
               CertificationCode = data.CertificationCode,
               CertificationName = data.CertificationName,
               ProjectPromptVector = contentVector
           };
    }
}

