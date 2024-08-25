using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator
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