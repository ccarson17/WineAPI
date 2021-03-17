using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class RackForUpdateDtoHateoas : IValidatableObject
    {
        [Required]
        [MaxLength(128)]
        public string guid { get; set; }
        public long rows { get; set; }
        public long cols { get; set; }
        [Required]
        [MaxLength(128)]
        public string owner_guid { get; set; }
        public string rack_name { get; set; }
        public long bottleCount { get; set; }
        public List<LinkDto> Links { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (rows < 0 || rows >= 10000)
                yield return new ValidationResult("rows value must be between 1 and 9999.", new[] { "RackForUpdateDto" });
            if (cols < 0 || cols >= 10000)
                yield return new ValidationResult("cols value must be between 1 and 9999.", new[] { "RackForUpdateDto" });
        }
    }
}
