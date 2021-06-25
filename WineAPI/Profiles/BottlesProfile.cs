using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WineAPI.Models;

namespace WineAPI.Profiles
{
    public class BottlesProfile : Profile
    {
        public BottlesProfile()
        {
            CreateMap<WineDataContext.tbl_Wine_Bottles_Item, Models.BottleDto>()
                .ForMember(
                    dest => dest.ABV,
                    opt => opt.MapFrom(src => String.Format("{0:0.##}", src.ABV) + "%"))
                .ForMember(
                    dest => dest.Size,
                    opt => opt.MapFrom(src => src.SizeInML >= 1000 ? Math.Round(((decimal)src.SizeInML / 1000), 2, MidpointRounding.AwayFromZero) + "L" : src.SizeInML + "ml"))
                .ForMember(
                    dest => dest.Year,
                    opt => opt.MapFrom(src => src.Year.ToString()))
                ;
            CreateMap<WineDataContext.tbl_Wine_Bottles_Item, Models.BottleDtoHateoas>()
                .ForMember(
                    dest => dest.ABV,
                    opt => opt.MapFrom(src => String.Format("{0:0.##}", src.ABV) + "%"))
                .ForMember(
                    dest => dest.Size,
                    opt => opt.MapFrom(src => src.SizeInML >= 1000 ? Math.Round(((decimal)src.SizeInML / 1000), 2, MidpointRounding.AwayFromZero) + "L" : src.SizeInML + "ml"))
                .ForMember(
                    dest => dest.Year,
                    opt => opt.MapFrom(src => src.Year.ToString()))
                ;
            CreateMap<Models.BottleForCreationDto, WineDataContext.tbl_Wine_Bottles_Item>();
            CreateMap<Models.BottleForUpdateDto, WineDataContext.tbl_Wine_Bottles_Item>();
            CreateMap<Models.BottleForUpdateDto, Models.BottleForUpdateDtoHateoas>();
            CreateMap<Models.UserBottleForEzCreationDto, WineDataContext.tbl_Wine_Bottles_Item>();
            CreateMap<WineDataContext.tbl_Wine_Bottles_Item, Models.UserBottleForEzCreationDto>();
        }
    }
}
