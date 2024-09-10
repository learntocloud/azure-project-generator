using Azure;
using azure_project_generator.models;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using OpenAI.Chat;
using Newtonsoft.Json;

namespace azure_project_generator.services
{
    public class ContentGenerationService
    {
        private readonly ILogger<ContentGenerationService> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly ChatClient _completionsClient;
        public ContentGenerationService(ILogger<ContentGenerationService> logger, EmbeddingClient embeddingClient, ChatClient completionsClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
            _completionsClient = completionsClient ?? throw new ArgumentNullException(nameof(completionsClient));
        }

        public string GenerateCertServiceContextSentence(CertificationService data) =>
            $"The {data.CertificationCode} {data.CertificationName} certification includes the skill of {data.SkillName}. Within this skill, there is a focus on the topic of {data.TopicName}, particularly through the use of the service {data.ServiceName}.";

        public string GenerateCertDataContextSentence(Certification data) =>
            $"The {data.CertificationCode} {data.CertificationName} certification includes the following skills: {string.Join(", ", data.SkillsMeasured.Select(s => s.Name))}.";
        public async Task<float[]> GenerateEmbeddingsAsync(string content)
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

        public async Task<string> GenerateProjectIdeaAsync(List<string> services, string skill, string topic)
        {
            string userPrompt = $@"You are a cloud architect specializing in Azure architecture.
                    Please generate a detailed project idea for a small, practical cloud solution based on the following Azure certification skill: {skill} and topic: {topic}.
                    The project should utilize ONLY the following services: {services}.
                    The project should focus on key technical steps without any subjective descriptions or recommendations.
                    The response must be formatted as valid JSON and include only the following fields:
                                {{
                                    ""title"": ""A concise, descriptive project name"",
                        ""description"": ""A factual description of the project, highlighting its technical purpose and main features."",
                        ""steps"": [
                            ""Step 1: Description of the first technical step"",
                            ""Step 2: Description of the second technical step"",
                            ""Step 3: Description of the third technical step"",
                            ""Step 4: Description of the fourth technical step"",
                            ""Step 5: Description of the fifth technical step""
                                    ]
                                }}
                             Ensure that the project idea is focused purely on technical details, aligned with best practices in Azure architecture, and small in scope.";

            try
            {
                _logger.LogInformation("Generating project idea...");
                ChatCompletion completion = await _completionsClient.CompleteChatAsync(new ChatMessage[]
                {
                new SystemChatMessage("You are a cloud engineer and mentor specialized in generating " +
                "beginner-friendly cloud project ideas. Provide the response in JSON format only, without any additional text."),
                new UserChatMessage(userPrompt)
                });

                string projectIdeaContent = completion.Content[0].Text;

                if (string.IsNullOrWhiteSpace(projectIdeaContent))
                {
                    _logger.LogWarning("The response from the AI model was empty or null.");
                    throw new Exception("Failed to generate project idea.");
                }

                string cleanedJsonContent = projectIdeaContent
                    .Replace("```json", string.Empty)
                    .Replace("```", string.Empty)
                    .Replace("json\n", string.Empty)
                    .Trim();

                // deserialize to CloudProjectIdea object

                CloudProjectIdea? cloudProjectIdea = JsonConvert.DeserializeObject<CloudProjectIdea>(cleanedJsonContent);
                if (cloudProjectIdea == null)
                {
                    _logger.LogError("Deserialization returned null for project idea.");
                    throw new Exception("Failed to deserialize project idea.");
                }
                // Ensure Steps is initialized if it's null
                cloudProjectIdea.Steps ??= new List<string>();

                foreach (var service in services)
                {
                    // Properly encode the skill, topic, and service
                    string encodedTopic = System.Net.WebUtility.UrlEncode(topic);
                    string encodedService = System.Net.WebUtility.UrlEncode(service);

                    // Construct the URL using the encoded values
                    cloudProjectIdea.Resources.Add($"https://learn.microsoft.com/search/?terms={encodedTopic}%20{encodedService}&category=Training");
                }

                
                string jsonString = JsonConvert.SerializeObject(cloudProjectIdea);

                return jsonString;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error parsing JSON response");
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure OpenAI API request failed");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project idea");
                throw;
            }
        }
    }
}