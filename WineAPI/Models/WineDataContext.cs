using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WineAPI.Models
{
    public class WineDataContext : DbContext
    {
        public WineDataContext(DbContextOptions<WineDataContext> options, IConfiguration configuration) : base(options)
        {
            Database.SetCommandTimeout(180);
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public DbSet<lk_Okta_API_Key_Item> lk_Okta_API_Key { get; set; }
        public DbSet<tbl_Wine_Bottles_Item> tbl_Wine_Bottles { get; set; }
        public DbSet<tbl_Wine_Racks_Item> tbl_Wine_Racks { get; set; }
        public DbSet<tbl_Rack_Contents_Item> tbl_Rack_Contents { get; set; }

        public class lk_Okta_API_Key_Item
        {
            [Key]
            public string API_Key { get; set; }
        }

        public class tbl_Wine_Bottles_Item
        {
            [Key]
            public string guid { get; set; }
            public string owner_guid { get; set; }
            public int? Year { get; set; }
            public string Vintner { get; set; }
            public string WineName { get; set; }
            public string Category { get; set; }
            public string Varietal { get; set; }
            public string City_Town { get; set; }
            public string Region { get; set; }
            public string State_Province { get; set; }
            public string Country { get; set; }
            public string ExpertRatings { get; set; }
            public int SizeInML { get; set; }
            public Decimal? ABV { get; set; }
            public string WinemakerNotes { get; set; }
        }

        public class tbl_Wine_Racks_Item
        {
            [Key]
            public string guid { get; set; }
            public long rows { get; set; }
            public long cols { get; set; }
            public string owner_guid { get; set; }
            public string rack_name { get; set; }
        }

        public class tbl_Rack_Contents_Item
        {
            [Key]
            public string guid { get; set; }
            public string owner_guid { get; set; }
            public string rack_guid { get; set; }
            public string rack_name { get; set; }
            public string bottle_guid { get; set; }
            public long rack_col { get; set; }
            public long rack_row { get; set; }
            public string where_bought { get; set; }
            public string price_paid { get; set; }
            public int? user_rating { get; set; }
            public DateTime? drink_date { get; set; }
            public string user_notes { get; set; }
            public DateTime? created_date { get; set; }
        }
    }
}