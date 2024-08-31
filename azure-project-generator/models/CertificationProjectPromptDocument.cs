using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace azure_project_generator.models
{
    public class CertificationProjectPromptDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }  // Certification code, also used as the ID
        [JsonPropertyName("certificationCode")]
        public string CertificationCode { get; set; }
        [JsonPropertyName("certificationName")]
        public string CertificationName { get; set; }
        [JsonPropertyName("skillName")]
        public string ProjectPrompt { get; set; }
        [JsonPropertyName("projectPrompt")]
        public float[] ProjectPromptVector { get; set; }
    }
}
