
using Azure;
using azure_project_generator.models;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using OpenAI.Chat;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace azure_project_generator.services
{
    public class ContentGenerationService
    {
        private readonly ILogger<ContentGenerationService> _logger;
        private readonly EmbeddingClient _embeddingClient;
        private readonly ChatClient _completionsClient;

        private const string JSON_FORMAT = @"{{
            ""title"": ""A concise, descriptive project name"",
            ""description"": ""A factual description of the project, highlighting its technical purpose and main features."",
            ""steps"": [
                ""Step 1: Description of the first technical step"",
                ""Step 2: Description of the second technical step"",
                ""Step 3: Description of the third technical step"",
                ""Step 4: Description of the fourth technical step"",
                ""Step 5: Description of the fifth technical step""
            ]
        }}";

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

        public async Task<string> GenerateProjectIdeaFromCertAsync(List<string> services, string skill, string topic)
        {
            string userPrompt = $@"You are a cloud architect specializing in Azure architecture.
                Please generate a detailed project idea for a small, practical cloud solution based on the following Azure certification skill: {skill} and topic: {topic}.
                The project should utilize ONLY the following services: {string.Join(", ", services)}.
                The project should focus on key technical steps without any subjective descriptions or recommendations.
                The response must be formatted as valid JSON and include only the following fields:
                {JSON_FORMAT}
                Ensure that the project idea is focused purely on technical details, aligned with best practices in Azure architecture, and small in scope.";

            return await GenerateProjectIdeaAsync(userPrompt, services, topic);
        }

        public async Task<string> GenerateProjectIdeaFromConceptAsync(List<string> services, string topic)
        {
            string userPrompt = $@"You are a cloud architect.
                Please generate a detailed project idea for a small, practical cloud solution based on the following cloud engineering concept certification: {topic}.
                The project should utilize ONLY the following services: {string.Join(", ", services)}.
                The project should focus on key technical steps without any subjective descriptions or recommendations.
                The response must be formatted as valid JSON and include only the following fields:
                {JSON_FORMAT}
                Ensure that the project idea is focused purely on technical details, aligned with best practices in cloud architecture, and small in scope.";

            return await GenerateProjectIdeaAsync(userPrompt, services, topic);
        }

        private async Task<string> GenerateProjectIdeaAsync(string userPrompt, List<string> services, string topic)
        {
            try
            {
                _logger.LogInformation("Generating project idea...");
                ChatCompletion completion = await _completionsClient.CompleteChatAsync(new ChatMessage[]
                {
                    new SystemChatMessage("You are a cloud engineer and mentor specialized in generating beginner-friendly cloud project ideas. Provide the response in JSON format only, without any additional text."),
                    new UserChatMessage(userPrompt)
                });

                string projectIdeaContent = completion.Content[0].Text;

                if (string.IsNullOrWhiteSpace(projectIdeaContent))
                {
                    _logger.LogWarning("The response from the AI model was empty or null.");
                    throw new Exception("Failed to generate project idea.");
                }

                string cleanedJsonContent = CleanJsonContent(projectIdeaContent);

                CloudProjectIdea? cloudProjectIdea = JsonConvert.DeserializeObject<CloudProjectIdea>(cleanedJsonContent);
                if (cloudProjectIdea == null)
                {
                    _logger.LogError("Deserialization returned null for project idea.");
                    throw new Exception("Failed to deserialize project idea.");
                }

                cloudProjectIdea.Steps ??= new List<string>();
                cloudProjectIdea.Resources = GenerateResources(services, topic);

                return JsonConvert.SerializeObject(cloudProjectIdea);
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

        private string CleanJsonContent(string content)
        {
            return Regex.Replace(content, @"^```json\s*|\s*```$", "", RegexOptions.Multiline).Trim();
        }

        private List<string> GenerateResources(List<string> services, string topic)
        {
            return services.Select(service =>
                $"https://learn.microsoft.com/search/?terms={System.Net.WebUtility.UrlEncode(topic)}%20{System.Net.WebUtility.UrlEncode(service)}&category=Training"
            ).ToList();
        }
    }
}