using System.ComponentModel.DataAnnotations;

namespace azure_project_generator.models
{
    public class SkillMeasured
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Percentage { get; set; }
        [Required]
        public Dictionary<string, List<string>> Topics { get; set; }
    }
}