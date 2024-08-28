using Azure.AI.OpenAI;
using Azure;
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
        public ProcessFile(ILogger<ProcessFile> logger)
        {
            _logger = logger;
            // Initialize and validate environment variables
            string keyFromEnvironment = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            string endpointFromEnvironment = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_ENDPOINT");
            string embeddingsDeployment = Environment.GetEnvironmentVariable("EMBEDDINGS_DEPLOYMENT");

            if (string.IsNullOrEmpty(keyFromEnvironment) || string.IsNullOrEmpty(endpointFromEnvironment) || string.IsNullOrEmpty(embeddingsDeployment))
            {
                _logger.LogError("Environment variables for Azure OpenAI API are not set properly.");
                throw new InvalidOperationException("Required environment variables are missing.");
            }

            // Initialize Azure OpenAI client
            AzureOpenAIClient azureClient = new(
                new Uri(endpointFromEnvironment),
                new AzureKeyCredential(keyFromEnvironment));

            _embeddingClient = azureClient.GetEmbeddingClient(embeddingsDeployment);

        }

        [Function(nameof(ProcessFile))]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainerOut%", Connection = "CosmosDBConnection")]
        public async Task<CertServiceDocument> Run(
            [BlobTrigger("certdata/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
        {
            string content;
            try
            {
                using var blobStreamReader = new StreamReader(stream);
                content = await blobStreamReader.ReadToEndAsync();
            }
            catch (IOException ex)
            {
                _logger.LogError($"Error reading blob content: {ex.Message}");
                return null;
            }

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name}");

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return null;
            }

            try
            {
                ValidateJsonContent(content);
            }
            catch (JsonReaderException ex)
            {
                _logger.LogError($"JSON parsing error: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
            }

            var mappedServiceData = JsonConvert.DeserializeObject<MappedService>(content);

            string contextSentence =
                $"The {mappedServiceData.CertificationCode} {mappedServiceData.CertificationName} certification includes the skill of {mappedServiceData.SkillName}. Within this skill, there is a focus on the topic of {mappedServiceData.TopicName}, particularly through the use of the service {mappedServiceData.ServiceName}.";

            List<float> contentVector = await GenerateEmbeddings(contextSentence);
            CertServiceDocument certServiceDocument = new CertServiceDocument();
            certServiceDocument.id = Guid.NewGuid().ToString();
            certServiceDocument.CertificationServiceKey = $"{mappedServiceData.CertificationCode}-{mappedServiceData.ServiceName}";
            certServiceDocument.CertificationCode = mappedServiceData.CertificationCode;
            certServiceDocument.CertificationName = mappedServiceData.CertificationName;
            certServiceDocument.SkillName = mappedServiceData.SkillName;
            certServiceDocument.TopicName = mappedServiceData.TopicName;
            certServiceDocument.ServiceName = mappedServiceData.ServiceName;
            certServiceDocument.ContextSentence = contextSentence;
            certServiceDocument.ContextVector = contentVector.ToArray();

            _logger.LogInformation("Document created successfully.");

            return certServiceDocument;

        }
        private async Task<List<float>> GenerateEmbeddings(string content)
        {
            try
            {
                _logger.LogInformation("Generating embedding...");
                var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(content).ConfigureAwait(false);
                List<float> embeddingVector = embeddingResult.Value.Vector.ToArray().ToList();
                _logger.LogInformation("Embedding created successfully.");
                return embeddingVector;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"Azure OpenAI API request failed: {ex.Message}");
                throw; // Re-throw the exception to ensure the caller is aware of the failure
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating embedding: {ex.Message}");
                throw; // Re-throw the exception to ensure the caller is aware of the failure
            }
        }
        private void ValidateJsonContent(string content)
        {
            var generator = new JSchemaGenerator();
            JSchema schema = generator.Generate(typeof(MappedService));

            JToken jsonContent = JToken.Parse(content);
            IList<string> messages;
            bool valid = jsonContent.IsValid(schema, out messages);

            if (!valid)
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
        }
    }
}
