using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class UserBottleDtoNoDetail
    {
        public string guid { get; set; }
        public string owner_guid { get; set; }
        public string rack_guid { get; set; }
        public long row { get; set; }
        public long col { get; set; }
        public string bottle_guid { get; set; }
        public string where_bought { get; set; }
        public string price_paid { get; set; }
        public int? user_rating { get; set; }
        public DateTime? drink_date { get; set; }
        public DateTime? created_date { get; set; }
        public string user_notes { get; set; }
    }
}
