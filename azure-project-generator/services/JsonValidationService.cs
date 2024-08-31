using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;


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
                JsonSchemaBuilder jsonSchemaBuilder = new JsonSchemaBuilder();
                var schema1 = jsonSchemaBuilder.FromType<T>().Build();

                var result = schema1.Evaluate(JsonNode.Parse(content));


                if (!result.IsValid)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Schema validation error: {error.Key} + \": \" + {error.Value}");
                    }
                }
                else
                {
                    _logger.LogInformation("JSON content is valid against the schema.");
                }

                return result.IsValid;
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
