using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class UserDto
    {
        public string street_address { get; set; }
        public string country { get; set; }
        public string website { get; set; }
        public string zoneinfo { get; set; }
        public string birthdate { get; set; }
        public string gender { get; set; }
        public string formatted { get; set; }
        public string profile { get; set; }
        public string locality { get; set; }
        public string given_name { get; set; }
        public string middle_name { get; set; }
        public string locale { get; set; }
        public string picture { get; set; }
        public string apiUserId { get; set; }
        public string name { get; set; }
        public string nickname { get; set; }
        public string phone_number { get; set; }
        public string region { get; set; }
        public string postal_code { get; set; }
        public string family_name { get; set; }
        public string email { get; set; }
    }
}
