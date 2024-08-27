using Azure.AI.OpenAI;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using OpenAI.Chat;
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
        public async Task Run([BlobTrigger("certdata/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name)
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
                return;
            }

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name}");

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Blob content is empty or whitespace.");
                return;
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



            // TODO generate embeddings


            var certificationData = JsonConvert.DeserializeObject<Certification>(content);
            await GenerateEmbeddings(certificationData.CertificationName);




        }

        private async Task GenerateEmbeddings(string content)
        {
            
            try
            {
                _logger.LogInformation("Generating embedding...");
                Embedding embedding = await _embeddingClient.GenerateEmbeddingAsync(content).ConfigureAwait(false);
                _logger.LogInformation("Embedding created successfully.");
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError($"Azure OpenAI API request failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating embedding: {ex.Message}");
            }
        }
                private void ValidateJsonContent(string content)
        {
            var generator = new JSchemaGenerator
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            JSchema schema = generator.Generate(typeof(Certification));

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
