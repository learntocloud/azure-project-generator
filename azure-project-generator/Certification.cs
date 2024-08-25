using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace azure_project_generator
{
    public class Certification
    {
        [Required]
        public string CertificationCode { get; set; }
        [Required]
        public string CertificationName { get; set; }
        [Required]
        public List<SkillMeasured> SkillsMeasured { get; set; }
    }

}
