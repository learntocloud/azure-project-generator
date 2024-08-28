using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using OpenAI.Embeddings;

namespace azure_project_generator
{
    public class ProcessFile
    {
        private readonly ILogger<ProcessFile> _logger;
        private readonly EmbeddingClient _embeddingClient;

        public ProcessFile(ILogger<ProcessFile> logger,
            EmbeddingClient embeddingClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        }

        [Function(nameof(ProcessFile))]
        public async Task<MultipleOutput> Run(
           [BlobTrigger("certdata/{name}", Connection = "AzureWebJobsStorage")] string content,
           string name)
        {
            _logger.LogInformation($"Processing blob: {name}");

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return new MultipleOutput { CertServiceDocument = null, ArchivedContent = null};
            }

            if (!ValidateJsonContent(content))
            {
                return new MultipleOutput { CertServiceDocument = null, ArchivedContent = null};
            }

            var mappedServiceData = JsonConvert.DeserializeObject<MappedService>(content);
            if (mappedServiceData == null)
            {
                _logger.LogError("Failed to deserialize content to MappedService.");
                return new MultipleOutput { CertServiceDocument = null, ArchivedContent = null};
            }

            string contextSentence = GenerateContextSentence(mappedServiceData);
            float[] contentVector = await GenerateEmbeddingsAsync(contextSentence);

            var certServiceDocument = CreateCertServiceDocument(mappedServiceData, contextSentence, contentVector);

            _logger.LogInformation("Document created successfully.");
            _logger.LogInformation($"Archiving blob: {name}");

            return new MultipleOutput
            {
                CertServiceDocument = certServiceDocument,
                ArchivedContent = content
               
            };
        }


        private async Task<string> ReadBlobContentAsync(Stream stream)
        {
            try
            {
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error reading blob content");
                return null;
            }
        }

        private bool ValidateJsonContent(string content)
        {
            try
            {
                var generator = new JSchemaGenerator();
                JSchema schema = generator.Generate(typeof(MappedService));

                JToken jsonContent = JToken.Parse(content);
                bool isValid = jsonContent.IsValid(schema, out IList<string> messages);

                if (!isValid)
                {
                    foreach (var message in messages)
                    {
                        _logger.LogError($"Schema validation error: {message}");
                    }
                }
                else
                {
                    _logger.LogInformation("JSON content is valid against the schema.");
                }

                return isValid;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error during validation");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during JSON validation");
                return false;
            }
        }

        private string GenerateContextSentence(MappedService data) =>
            $"The {data.CertificationCode} {data.CertificationName} certification includes the skill of {data.SkillName}. Within this skill, there is a focus on the topic of {data.TopicName}, particularly through the use of the service {data.ServiceName}.";

        private async Task<float[]> GenerateEmbeddingsAsync(string content)
        {
            try
            {
                _logger.LogInformation("Generating embedding...");
                var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(content).ConfigureAwait(false);
                _logger.LogInformation("Embedding created successfully.");
                return embeddingResult.Value.Vector.ToArray();
              
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure OpenAI API request failed");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding");
                throw;
            }
        }

        private CertServiceDocument CreateCertServiceDocument(MappedService data, string contextSentence, float[] contentVector) =>
            new CertServiceDocument
            {
                id = Guid.NewGuid().ToString(),
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