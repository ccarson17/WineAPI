using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class BottleCollection
    {
        public List<BottleDtoHateoas> values { get; set; }
        public List<LinkDto> links { get; set; }
        public BottleCollection(List<BottleDtoHateoas> _values, List<LinkDto> _links)
        {
            values = _values;
            links = _links;
        }
    }
}
