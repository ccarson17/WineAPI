using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class BottleDto
    {
        public string guid { get; set; }
        public string owner_guid { get; set; }
        public string Year { get; set; }
        public string Vintner { get; set; }
        public string WineName { get; set; }
        public string Category { get; set; }
        public string Varietal { get; set; }
        public string City_Town { get; set; }
        public string Region { get; set; }
        public string State_Province { get; set; }
        public string Country { get; set; }
        public string ExpertRatings { get; set; }
        public string Size { get; set; }
        public string ABV { get; set; }
        public string WinemakerNotes { get; set; }
    }
}
