using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using System.Text.Json;

namespace azure_project_generator.services
{
    public class JsonValidationService
    {
        private readonly ILogger<JsonValidationService> _logger;

        public JsonValidationService(ILogger<JsonValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool ValidateJsonContent<T>(string content)

        {
            try
            {
                var generator = new JSchemaGenerator();
                JSchema schema = generator.Generate(typeof(T));

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
    }
}
