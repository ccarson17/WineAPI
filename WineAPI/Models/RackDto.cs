using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class RackDto
    {
        public string guid { get; set; }
        public long rows { get; set; }
        public long cols { get; set; }
        public string owner_guid { get; set; }
        public string rack_name { get; set; }
        public long bottleCount { get; set; }
        public Dictionary<string, BottleDto> bottles { get; set; }
            = new Dictionary<string, BottleDto>();
    }
}
