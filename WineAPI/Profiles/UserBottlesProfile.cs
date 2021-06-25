using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WineAPI.Models;

namespace WineAPI.Profiles
{
    public class UserBottlesProfile : Profile
    {
        public UserBottlesProfile()
        {
            CreateMap<Models.UserBottleForCreationDto, WineDataContext.tbl_Rack_Contents_Item>();
            CreateMap<Models.UserBottleForEzCreationDto, WineDataContext.tbl_Rack_Contents_Item>();
            CreateMap<WineDataContext.tbl_Rack_Contents_Item, Models.UserBottleForUpdateDto>();
            CreateMap<WineDataContext.tbl_Rack_Contents_Item, Models.UserBottleForEzCreationDto>();
            CreateMap<Models.UserBottleForUpdateDto, WineDataContext.tbl_Rack_Contents_Item>();
            CreateMap<UserBottleDtoNoDetail, UserBottleDtoNoDetailHateoas>();
            CreateMap<UserBottleDto, UserBottleDtoHateoas>();

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
            CreateMap<WineDataContext.tbl_Wine_Bottles_Item, UserBottleDto>()
                .ForMember(
                    src => src.guid, 
                    opt => opt.Ignore())
                .ForMember(
                    dest => dest.bottle_guid,
                    opt => opt.MapFrom(src => src.guid))
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
            CreateMap<WineDataContext.tbl_Rack_Contents_Item, UserBottleDto>()
                .ForMember(
                    dest => dest.row,
                    opt => opt.MapFrom(src => src.rack_row))
                .ForMember(
                    dest => dest.col,
                    opt => opt.MapFrom(src => src.rack_col))
                ;
            CreateMap<WineDataContext.tbl_Rack_Contents_Item, UserBottleDtoNoDetail>()
                .ForMember(
                    dest => dest.row,
                    opt => opt.MapFrom(src => src.rack_row))
                .ForMember(
                    dest => dest.col,
                    opt => opt.MapFrom(src => src.rack_col))
                ;

            CreateMap<UserBottleForUpdateDto, UserBottleDto>()
                .ForMember(
                    dest => dest.row,
                    opt => opt.MapFrom(src => src.rack_row))
                .ForMember(
                    dest => dest.col,
                    opt => opt.MapFrom(src => src.rack_col))
                ;
            IMapper mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WineDataContext.tbl_Wine_Bottles_Item, UserBottleDto>();
                cfg.CreateMap<WineDataContext.tbl_Rack_Contents_Item, UserBottleDto>();
            }).CreateMapper();

            IMapper mapper2 = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<WineDataContext.tbl_Wine_Bottles_Item, UserBottleForUpdateDto>();
                cfg.CreateMap<WineDataContext.tbl_Rack_Contents_Item, UserBottleForUpdateDto>();
            }).CreateMapper();
        }
    }
}
