using Azure;
using azure_project_generator.models;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace azure_project_generator.services
{
    public class ContentGenerationService
    {
        private readonly ILogger<ContentGenerationService> _logger;
        private readonly EmbeddingClient _embeddingClient;

        public ContentGenerationService(ILogger<ContentGenerationService> logger, EmbeddingClient embeddingClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
        }

        public string GenerateCertServiceContextSentence(CertificationService data) =>
            $"The {data.CertificationCode} {data.CertificationName} certification includes the skill of {data.SkillName}. Within this skill, there is a focus on the topic of {data.TopicName}, particularly through the use of the service {data.ServiceName}.";

        public string GenerateCertDataContextSentence(Certification certificationDocument)
        {
            var certificationName = certificationDocument.CertificationName;
            var certificationCode = certificationDocument.CertificationCode;

            var skills = certificationDocument.SkillsMeasured.Select(s => s.Name).ToList();
            var topicsWithServices = certificationDocument.SkillsMeasured
                .SelectMany(s => s.Topics)
                .Select(t => $"{t.TopicName} (using {string.Join(", ", t.Services)})").ToList();

            string skillNames = string.Join(", ", skills);
            string topicDetails = string.Join("; ", topicsWithServices);

            string prompt = $"Generate a project idea that aligns with the {certificationCode} certification, " +
                            $"which covers skills such as {skillNames}. The project should focus on topics like {topicDetails}, " +
                            $"addressing real-world scenarios related to {certificationName}.";

            return prompt;

        }

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
    }
}
