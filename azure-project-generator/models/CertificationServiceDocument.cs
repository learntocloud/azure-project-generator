﻿using System.Text.Json.Serialization;

namespace azure_project_generator.models
{
    public class CertificationServiceDocument
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }  // Unique identifier for the document

        [JsonPropertyName("certificationServiceKey")]
        public string CertificationServiceKey { get; set; }  // Composite key
        [JsonPropertyName("certificationCode")]
        public string CertificationCode { get; set; }  // The certification code
        [JsonPropertyName("certificationName")]
        public string CertificationName { get; set; }  // The certification name
        [JsonPropertyName("skillName")]
        public string SkillName { get; set; }  // The skill associated with this certification
        [JsonPropertyName("topicName")]
        public string TopicName { get; set; }  // The topic within the skill
        [JsonPropertyName("serviceName")]
        public string ServiceName { get; set; }  // The service relevant to this certification and skill
        [JsonPropertyName("contextSentence")]
        public string ContextSentence { get; set; }  // The combined sentence
        [JsonPropertyName("contextVector")]
        public float[] ContextVector { get; set; }  // Example vector embedding generated from the sentence

    }
}
