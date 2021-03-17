using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WineAPI.Models;

namespace WineAPI.Profiles
{
    public class RacksProfile : Profile
    {
        public RacksProfile()
        {
            CreateMap<WineDataContext.tbl_Wine_Racks_Item, Models.RackDto>();
            CreateMap<Models.RackDto, Models.RackDtoHateoas>();
            CreateMap<WineDataContext.tbl_Wine_Racks_Item, Models.RackForUpdateDto>();
            CreateMap<Models.RackForCreationDto, WineDataContext.tbl_Wine_Racks_Item>();
            CreateMap<Models.RackForUpdateDto, WineDataContext.tbl_Wine_Racks_Item>();
            CreateMap<Models.RackForUpdateDto, Models.RackForUpdateDtoHateoas>();
        }
    }
}
