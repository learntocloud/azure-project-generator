using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator
{
    internal class MappedService
    {
        [JsonProperty("certificationCode")]
        public string CertificationCode { get; set; }

        [JsonProperty("certificationName")]
        public string CertificationName { get; set; }

        [JsonProperty("skillName")]
        public string SkillName { get; set; }

        [JsonProperty("topicName")]
        public string TopicName { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }
    }
}
