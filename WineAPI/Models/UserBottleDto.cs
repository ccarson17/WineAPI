using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class UserBottleDto
    {
        public string guid { get; set; }
        public string owner_guid { get; set; }
        public string rack_guid { get; set; }
        public string rack_name { get; set; }
        public long row { get; set; }
        public long col { get; set; }
        public string bottle_guid { get; set; }
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
        public string where_bought { get; set; }
        public string price_paid { get; set; }
        public int? user_rating { get; set; }
        public DateTime? drink_date { get; set; }
        public DateTime? created_date { get; set; }
        public string user_notes { get; set; }
    }
}
