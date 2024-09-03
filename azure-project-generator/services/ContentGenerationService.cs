using Azure;
using azure_project_generator.models;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using OpenAI.Chat;
using Newtonsoft.Json.Linq;

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

        public async Task<string> GenerateProjectIdeaAsync(string services, string skill)
        {
            string userPrompt = $@"You are an expert cloud architect. 
Please generate a detailed project idea for a beginner-friendly weekend cloud solution 
based on the following Azure certification skill: {skill}.
The project should utilize the ONLY following services: {services}.
The project should be small in scale, achievable over a weekend, and have a fun, creative name. Suitable for beginners. Cheap to run.
The response must be formatted as valid JSON and include only the following fields:
{{
    ""projectName"": ""A fun and creative project name"",
    ""description"": ""A brief, engaging description of the project, highlighting its purpose and main features."",
    ""learningGoals"": [""Goal 1"", ""Goal 2"", ""Goal 3""],
    ""steps"": [
        ""Step 1: Description of the first step"",
        ""Step 2: Description of the second step"",
        ""Step 3: Description of the third step"",
        ""Step 4: Description of the fourth step"",
        ""Step 5: Description of the fifth step""
    ]
}}
Ensure that the project idea is practical, aligned with beginner-level skills, and leverages best practices in Azure architecture.  Small in scope";

            try
            {
                _logger.LogInformation("Generating project idea...");
                ChatCompletion completion = await _completionsClient.CompleteChatAsync(new ChatMessage[]
                {
                new SystemChatMessage("You are a technical assistant specialized in generating beginner-friendly cloud project ideas. Provide the response in JSON format only, without any additional text."),
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

                JObject jsonObject = JObject.Parse(cleanedJsonContent);

                // Validate that all required fields are present
                string[] requiredFields = { "projectName", "description", "learningGoals", "steps"};
                foreach (var field in requiredFields)
                {
                    if (!jsonObject.ContainsKey(field))
                    {
                        _logger.LogWarning($"Generated JSON is missing required field: {field}");
                        throw new System.Text.Json.JsonException($"Generated project idea is missing required field: {field}");

                    }
                }

                return jsonObject.ToString();
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
