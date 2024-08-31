using Newtonsoft.Json;

namespace azure_project_generator.models
{

    public class Certification
    {
        [JsonProperty("certificationCode")]
        public string CertificationCode { get; set; }
        [JsonProperty("certificationName")]
        public string CertificationName { get; set; }
        [JsonProperty("skillsMeasured")]
        public List<Skill> SkillsMeasured { get; set; }
    }

    public class Skill
    {
        [JsonProperty("name")]

        public string Name { get; set; }
        [JsonProperty("percentage")]
        public string Percentage { get; set; }
        [JsonProperty("topics")]
        public List<Topic> Topics { get; set; }
    }

    public class Topic
    {
        [JsonProperty("topicName")]
        public string TopicName { get; set; }
        [JsonProperty("services")]
        public List<string> Services { get; set; }
    }


}
