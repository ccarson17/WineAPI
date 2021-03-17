using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class UserBottleForCreationDto //: IValidatableObject
    {
        [Required]
        [MaxLength(128)]
        public string owner_guid { get; set; }
        [MaxLength(128)]
        public string rack_guid { get; set; }
        [Required]
        [MaxLength(128)]
        public string bottle_guid { get; set; }
        public string rack_name { get; set; }
        public long rack_col { get; set; }
        public long rack_row { get; set; }
        public string where_bought { get; set; }
        public string price_paid { get; set; }
        public int? user_rating { get; set; }
        public DateTime? drink_date { get; set; }
        public DateTime? created_date { get; set; }
        public string user_notes { get; set; }
        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    if (ABV > 100 || ABV < 0)
        //        yield return new ValidationResult("ABV must be between 0 and 100", new[] { "BottleForUpdateDto" });
        //    else if (Year > DateTime.UtcNow.Year + 1)
        //        yield return new ValidationResult("Year cannot be in the future. Enter 0 for non-vintage wines.", new[] { "BottleForUpdateDto" });
        //}
    }
}
