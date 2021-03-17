using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.ResourceParameters
{
    public class BottlesResourceParameters
    {
        const int maxPageSize = 250;
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
        private int _pageSize = 10;
        public int pageSize { 
            get => _pageSize;
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value;
        }
        public int pageNumber { get; set; } = 1;
        public string searchQuery { get; set; }
        public string orderBy { get; set; } = "Vintner, Varietal, Year, WineName";
    }
}
