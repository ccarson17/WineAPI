using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class RackForCreationDto : IValidatableObject
    {
        public string guid { get; set; }
        [Required]
        public long rows { get; set; }
        [Required]
        public long cols { get; set; }
        [Required]
        [MaxLength(128)]
        public string owner_guid { get; set; }
        [MaxLength(128)]
        public string rack_name { get; set; }
        public long bottleCount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (rows <= 0 || rows >= 10000)
                yield return new ValidationResult("rows value must be between 1 and 9999.", new[] { "RackForCreationDto" });
            if (cols <= 0 || cols >= 10000)
                yield return new ValidationResult("cols value must be between 1 and 9999.", new[] { "RackForCreationDto" });
        }
    }
}
