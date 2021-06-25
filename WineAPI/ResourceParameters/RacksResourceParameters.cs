using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.ResourceParameters
{
    public class RacksResourceParameters
    {
        const int maxPageSize = 250;
        [MaxLength(128)]
        public string guid { get; set; }
        public long rows { get; set; }
        public long cols { get; set; }
        [Required]
        [MaxLength(128)]
        public string owner_guid { get; set; }
        [MaxLength(128)]
        public string rack_name { get; set; }
        //public long bottleCount { get; set; }
        private int _pageSize = 10;
        public int pageSize
        {
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
        public int pageNumber { get; set; } = 1;
        public string orderBy { get; set; } = "guid";

    }
}
