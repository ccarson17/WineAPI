using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class UserBottleForUpdateDto : IValidatableObject
    {
        [Required]
        [MaxLength(128)]
        public string guid { get; set; }
        [Required]
        [MaxLength(128)]
        public string owner_guid { get; set; }
        [MaxLength(128)]
        public string rack_guid { get; set; }
        [MaxLength(128)]
        public string rack_name { get; set; }
        [Required]
        [MaxLength(128)]
        public string bottle_guid { get; set; }
        public long rack_col { get; set; }
        public long rack_row { get; set; }
        public string where_bought { get; set; }
        public string price_paid { get; set; }
        public int? user_rating { get; set; }
        public DateTime? drink_date { get; set; }
        public DateTime? created_date { get; set; }
        public string user_notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (user_rating != null && (user_rating < 1 || user_rating > 10))
                yield return new ValidationResult("User rating must be between 1 and 10", new[] { "UserBottleForUpdateDto" });
            if (rack_guid != null)
            {
                if (rack_col <= 0 || rack_row <= 0)
                    yield return new ValidationResult("Rack coordinates " + rack_row + "," + rack_col + " are invalid for rack: " + rack_guid, new[] { "UserBottleForUpdateDto" });
                if (rack_guid.StartsWith("invalid"))
                    yield return new ValidationResult("Error: " + rack_guid, new[] { "UserBottleForUpdateDto" });
                if (bottle_guid.StartsWith("invalid"))
                    yield return new ValidationResult("Error: invalid bottle_guid", new[] { "UserBottleForUpdateDto" });
            }
            if(drink_date != null && (rack_guid != null || rack_row != 0 || rack_col != 0))
                yield return new ValidationResult("If drink_date is set to a value: rack_guid must be null, rack_row must be 0 and rack_col must be 0" + rack_guid, new[] { "UserBottleForUpdateDto" });
        }
    }
}
