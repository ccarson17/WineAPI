﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class BottleForCreationDto : IValidatableObject
    {
        public string guid { get; set; }
        public string owner_guid { get; set; }
        [Required]
        public int? Year { get; set; }
        [Required]
        [MaxLength(128)]
        public string Vintner { get; set; }
        [Required]
        [MaxLength(128)]
        public string WineName { get; set; }
        [Required]
        [MaxLength(128)]
        public string Category { get; set; }
        [Required]
        [MaxLength(128)]
        public string Varietal { get; set; }
        public string City_Town { get; set; }
        public string Region { get; set; }
        public string State_Province { get; set; }
        public string Country { get; set; }
        public string ExpertRatings { get; set; }
        [Required]
        public int SizeInML { get; set; }
        public Decimal? ABV { get; set; }
        public string WinemakerNotes { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ABV > 100 || ABV < 0)
                yield return new ValidationResult("ABV must be between 0 and 100", new[] { "BottleForUpdateDto" });
            else if (Year > DateTime.UtcNow.Year + 1)
                yield return new ValidationResult("Year cannot be in the future. Enter 0 for non-vintage wines.", new[] { "BottleForUpdateDto" });
        }
    }
}
