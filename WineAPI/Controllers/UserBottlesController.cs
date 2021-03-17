using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using WineAPI.Helpers;
using WineAPI.Models;
using WineAPI.ResourceParameters;

namespace WineAPI.Controllers
{
    [ApiController]
    [Route("api/userbottles/")]
    public class UserBottlesController : ControllerBase
    {
        private readonly WineDataContext _wineData;
        private readonly IMapper _mapper;

        public UserBottlesController(WineDataContext wineData, IMapper mapper)
        {
            _wineData = wineData ??
                throw new ArgumentNullException(nameof(wineData));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = "GetUserBottles")]
        [HttpHead]
        public IActionResult GetUserBottles([FromQuery] UserBottlesResourceParameters parameters, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            //{
            //    return BadRequest();
            //}

            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            //Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            //Response.Headers[HeaderNames.Expires] = "0";
            //Response.Headers[HeaderNames.Pragma] = "no-cache";

            if (parameters.owner_guid == null) return BadRequest("Owner GUID must be provided.");
            var collection = _wineData.tbl_Rack_Contents.Where(x => x.owner_guid == parameters.owner_guid);
            if (parameters.guid != null) collection = collection.Where(x => x.guid == parameters.guid);
            if (parameters.rack_guid != null) collection = collection.Where(x => x.rack_guid == parameters.rack_guid);
            if (parameters.bottle_guid != null) collection = collection.Where(x => x.bottle_guid == parameters.bottle_guid);
            if (parameters.rack_col != 0) collection = collection.Where(x => x.rack_col == parameters.rack_col);
            if (parameters.rack_row != 0) collection = collection.Where(x => x.rack_row == parameters.rack_row);
            if (parameters.getCurrentOrHistory == "current") collection = collection.Where(x => x.drink_date == null);
            else if (parameters.getCurrentOrHistory == "history") collection = collection.Where(x => x.drink_date != null);
            // if user doesn't want bottle details, simply return now
            if (parameters.skip_details)
            {
                List<UserBottleDtoNoDetail> small_output = _mapper.Map<List<UserBottleDtoNoDetail>>(collection.ToList());
                PagedList<UserBottleDtoNoDetail> returnListSm = new PagedList<UserBottleDtoNoDetail>(small_output, small_output.Count(), parameters.pageNumber, parameters.pageSize);

                var previousPageLinkSm = returnListSm.HasPrevious ? CreateBottleResourceUri(parameters, ResourceUriType.PreviousPage) : null;
                var nextPageLinkSm = returnListSm.HasNext ? CreateBottleResourceUri(parameters, ResourceUriType.NextPage) : null;
                var paginationMetadataSm = new
                {
                    totalCount = returnListSm.TotalCount,
                    pageSize = returnListSm.PageSize,
                    currentPage = returnListSm.CurrentPage,
                    totalPages = returnListSm.TotalPages,
                    previousPageLink = previousPageLinkSm,
                    nextPageLink = nextPageLinkSm
                };
                Response.Headers.Add("X-Pagination",
                    System.Text.Json.JsonSerializer.Serialize(paginationMetadataSm));

                if (small_output.Count == 0)
                    return NotFound();
                else
                {
                    if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
                    {
                        var small_output_hateoas = _mapper.Map<List<UserBottleDtoNoDetailHateoas>>(small_output);
                        foreach (var item in small_output_hateoas)
                        {
                            item.Links = CreateLinksForUserBottle(Guid.Parse(item.guid)).ToList();
                        }
                        PagedList<UserBottleDtoNoDetailHateoas> returnListSmHateoas = new PagedList<UserBottleDtoNoDetailHateoas>(small_output_hateoas, small_output_hateoas.Count(), parameters.pageNumber, parameters.pageSize);
                        return Ok(returnListSmHateoas.ToList());
                    }
                    else
                    {
                        return Ok(returnListSm.ToList());
                    }
                }
            }
            // if user wants details, join in bottle info and return
            var joined_collection = collection.Join(_wineData.tbl_Wine_Bottles, rack => rack.bottle_guid, bottle => bottle.guid, (rack, bottle) => new { Rack = rack, Bottle = bottle });
            if (parameters.Category != null)
                joined_collection = joined_collection.Where(x => x.Bottle.Category == parameters.Category);
            if (parameters.Vintner != null)
                joined_collection = joined_collection.Where(x => x.Bottle.Vintner == parameters.Vintner);
            if (parameters.Varietal != null)
                joined_collection = joined_collection.Where(x => x.Bottle.Varietal == parameters.Varietal);
            if (parameters.WineName != null)
                joined_collection = joined_collection.Where(x => x.Bottle.WineName == parameters.WineName);
            if (parameters.City_Town != null)
                joined_collection = joined_collection.Where(x => x.Bottle.City_Town == parameters.City_Town);
            if (parameters.Region != null)
                joined_collection = joined_collection.Where(x => x.Bottle.Region == parameters.Region);
            if (parameters.State_Province != null)
                joined_collection = joined_collection.Where(x => x.Bottle.State_Province == parameters.State_Province);
            if (parameters.Country != null)
                joined_collection = joined_collection.Where(x => x.Bottle.Country == parameters.Country);
            if (parameters.where_bought != null)
                joined_collection = joined_collection.Where(x => x.Rack.where_bought == parameters.where_bought);
            if(parameters.minYear != null)
            {
                int minYear = 0;
                int.TryParse(parameters.minYear, out minYear);
                joined_collection = joined_collection.Where(x => x.Bottle.Year >= minYear);
            }
            if (parameters.maxYear != null)
            {
                int maxYear = 0;
                int.TryParse(parameters.maxYear, out maxYear);
                joined_collection = joined_collection.Where(x => x.Bottle.Year <= maxYear);
            }
            if (parameters.minRating != null)
                joined_collection = joined_collection.Where(x => x.Rack.user_rating >= parameters.minRating);
            if (parameters.maxRating != null)
                joined_collection = joined_collection.Where(x => x.Rack.user_rating <= parameters.maxRating);
            if(!String.IsNullOrEmpty(parameters.searchQuery))
            {
                joined_collection = joined_collection.Where(x => x.Bottle.Category.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.Varietal.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.Vintner.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.City_Town.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.Region.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.State_Province.ToLower().Contains(parameters.searchQuery.ToLower()) ||
                    x.Bottle.Country.ToLower().Contains(parameters.searchQuery.ToLower()));
            }
            try
            {
                string modifiedOrderBy = parameters.orderBy.ToLower()
                    .Replace("rack_col", "rack.rack_col")
                    .Replace("rack_row", "rack.rack_row")
                    .Replace("rack_name", "rack.rack_name")
                    .Replace("where_bought", "rack.where_bought")
                    .Replace("price_paid", "rack.price_paid")
                    //.Replace("price_paid asc", "(rack.price_paid ?? Int32.MaxValue) asc")
                    //.Replace("price_paid desc", "(rack.price_paid ?? Int32.MinValue) desc")
                    .Replace("user_rating", "rack.user_rating")
                    //.Replace("user_rating asc", "(rack.user_rating ?? Int32.MaxValue) asc")
                    //.Replace("user_rating desc", "(rack.user_rating ?? Int32.MinValue) desc")
                    .Replace("drink_date", "rack.drink_date")
                    .Replace("created_date", "rack.created_date")
                    .Replace("user_notes", "rack.user_notes")
                    .Replace("year", "bottle.year")
                    .Replace("vintner", "bottle.vintner")
                    .Replace("winename", "bottle.winename")
                    .Replace("category", "bottle.category")
                    .Replace("varietal", "bottle.varietal")
                    .Replace("city_town", "bottle.city_town")
                    .Replace("region", "bottle.region")
                    .Replace("state_province", "bottle.state_province")
                    .Replace("country", "bottle.country")
                    .Replace("expertratings", "bottle.expertratings")
                    .Replace("size", "bottle.size")
                    .Replace("abv", "bottle.abv")
                    .Replace("winemakernotes", "bottle.winemakernotes");
                joined_collection = joined_collection.OrderBy(modifiedOrderBy);
            }
            catch(Exception ex)
            {
                return BadRequest();
            }
            List<UserBottleDto> output = new List<UserBottleDto>();
            foreach (var item in joined_collection)
            {
                bool include = true;
                if(parameters.minPrice != null)
                {
                    include = !(String.IsNullOrEmpty(item.Rack.price_paid) || extractNumber(item.Rack.price_paid) < extractNumber(parameters.minPrice));
                }
                if (parameters.maxPrice != null && include)
                {
                    include = !(String.IsNullOrEmpty(item.Rack.price_paid) || extractNumber(item.Rack.price_paid) > extractNumber(parameters.maxPrice));
                }
                if (include)
                    output.Add(_mapper.MergeInto<UserBottleDto>(item.Rack, item.Bottle));
            }
            PagedList<UserBottleDto> returnList = new PagedList<UserBottleDto>(output, output.Count(), parameters.pageNumber, parameters.pageSize);

            var previousPageLink = returnList.HasPrevious ? CreateBottleResourceUri(parameters, ResourceUriType.PreviousPage) : null;
            var nextPageLink = returnList.HasNext ? CreateBottleResourceUri(parameters, ResourceUriType.NextPage) : null;
            var paginationMetadata = new
            {
                totalCount = returnList.TotalCount,
                pageSize = returnList.PageSize,
                currentPage = returnList.CurrentPage,
                totalPages = returnList.TotalPages,
                previousPageLink,
                nextPageLink,
                parameters.orderBy
            };
            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(paginationMetadata));

            if (output.Count == 0)
                return NotFound();
            else
            {
                if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
                {
                    var output_hateoas = _mapper.Map<List<UserBottleDtoHateoas>>(output);
                    foreach(var item in output_hateoas)
                    {
                        item.Links = CreateLinksForUserBottle(Guid.Parse(item.guid)).ToList();
                    }
                    PagedList<UserBottleDtoHateoas> returnListHateoas = new PagedList<UserBottleDtoHateoas>(output_hateoas, output_hateoas.Count(), parameters.pageNumber, parameters.pageSize);
                    return Ok(returnListHateoas.ToList());
                }
                else
                    return Ok(returnList.ToList());
            }
        }

        [HttpGet("{userBottleId}", Name = "GetUserBottle")]
        public IActionResult GetUserBottle(Guid userBottleId, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            //{
            //    return BadRequest();
            //}

            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            //Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            //Response.Headers[HeaderNames.Expires] = "0";
            //Response.Headers[HeaderNames.Pragma] = "no-cache";

            string userBottleIdStr = "";
            if (userBottleId != null) userBottleIdStr = userBottleId.ToString();
            var wineFromDB = _wineData.tbl_Rack_Contents.Where(x => x.guid == userBottleIdStr);
            if (wineFromDB.Count() == 0) return NotFound();
            var joined_collection = wineFromDB.Join(_wineData.tbl_Wine_Bottles, rack => rack.bottle_guid, bottle => bottle.guid, (rack, bottle) => new { Rack = rack, Bottle = bottle }).FirstOrDefault();
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var mappedBottle = _mapper.MergeInto<UserBottleDto>(joined_collection.Rack, joined_collection.Bottle);
                var mappedBottleHateoas = _mapper.Map<UserBottleDtoHateoas>(mappedBottle);
                mappedBottleHateoas.Links = CreateLinksForUserBottle(Guid.Parse(mappedBottleHateoas.guid)).ToList();
                return Ok(mappedBottleHateoas);
            }
            else
                return Ok(_mapper.MergeInto<UserBottleDto>(joined_collection.Rack, joined_collection.Bottle));
        }

        [HttpPost(Name = "CreateUserBottle")]
        public ActionResult<UserBottleDto> CreateUserBottle(UserBottleForCreationDto userbottle, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            //{
            //    return BadRequest();
            //}

            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            //Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            //Response.Headers[HeaderNames.Expires] = "0";
            //Response.Headers[HeaderNames.Pragma] = "no-cache";

            var userBottleEntity = _mapper.Map<WineDataContext.tbl_Rack_Contents_Item>(userbottle);
            if (userbottle.rack_guid != null)
            {
                var existingRack = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == userbottle.owner_guid && x.guid == userbottle.rack_guid).ToList();
                if (existingRack.Count() == 0)
                    return BadRequest("Rack guid not found: " + userbottle.rack_guid);
                var existingBottles = _wineData.tbl_Rack_Contents.Where(x => x.owner_guid == userbottle.owner_guid && x.rack_guid == userbottle.rack_guid &&
                    x.rack_row == userbottle.rack_row && x.rack_col == userbottle.rack_col).ToList();
                if (existingBottles.Count() != 0)
                    return BadRequest("Rack guid " + userbottle.rack_guid + " already contains a bottle at position " + userbottle.rack_row + "," + userbottle.rack_col);
                if (existingRack.FirstOrDefault().rows < userbottle.rack_row || existingRack.FirstOrDefault().cols < userbottle.rack_col)
                    return BadRequest("Position " + userbottle.rack_row + "," + userbottle.rack_col + " does not exist for rack guid " + userbottle.rack_guid);
                if (userbottle.rack_row <= 0 || userbottle.rack_col < 0)
                    return BadRequest("If a rack guid is supplied, rack_row and rack_col must be greater than 0");
            }
            var bottle = _wineData.tbl_Wine_Bottles.Where(x => x.guid == userbottle.bottle_guid).FirstOrDefault();
            if (bottle == null) return BadRequest("Bottle guid not found: " + userbottle.bottle_guid);
            userBottleEntity.guid = Guid.NewGuid().ToString();
            _wineData.tbl_Rack_Contents.Add(userBottleEntity);
            _wineData.SaveChanges();
            var userBottleToReturn = _mapper.MergeInto<UserBottleDto>(userBottleEntity, bottle);
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var mappedBottleHateoas = _mapper.Map<UserBottleDtoHateoas>(userBottleToReturn);
                mappedBottleHateoas.Links = CreateLinksForUserBottle(Guid.Parse(mappedBottleHateoas.guid)).ToList();
                return CreatedAtRoute("GetUserBottle", new { userBottleId = mappedBottleHateoas.guid }, mappedBottleHateoas);
            }
            else
                return CreatedAtRoute("GetUserBottle", new { userBottleId = userBottleToReturn.guid }, userBottleToReturn);
        }

        [HttpPatch("{userBottleId}", Name = "UpdateUserBottle")]
        public ActionResult<UserBottleDto> UpdateUserBottle(Guid userBottleId, Guid ownerId, JsonPatchDocument<UserBottleForUpdateDto> patchDocument, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            //{
            //    return BadRequest();
            //}

            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            //Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            //Response.Headers[HeaderNames.Expires] = "0";
            //Response.Headers[HeaderNames.Pragma] = "no-cache";

            if (userBottleId == null || ownerId == null)
                return BadRequest("Error: guid and owner_guid are required");
            var bottleToUpdateEntity = _wineData.tbl_Rack_Contents.Where(x => x.guid == userBottleId.ToString() && x.owner_guid == ownerId.ToString()).FirstOrDefault();
            if (bottleToUpdateEntity == null)
                return BadRequest("Bottle guid not found: " + userBottleId);
            var bottleToUpdate = _mapper.Map<UserBottleForUpdateDto>(bottleToUpdateEntity);
            _wineData.Entry(bottleToUpdateEntity).State = EntityState.Detached;
            patchDocument.ApplyTo(bottleToUpdate);
            var bottleToUpdateEntityOutput = _mapper.Map<WineDataContext.tbl_Rack_Contents_Item>(bottleToUpdate);
            if (bottleToUpdate.rack_guid != null)
            {
                var existingRack = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == ownerId.ToString() && x.guid == bottleToUpdate.rack_guid).ToList();
                if (existingRack.Count() == 0) bottleToUpdate.rack_guid = null;
                else if (existingRack.FirstOrDefault().cols < bottleToUpdate.rack_col || existingRack.FirstOrDefault().rows < bottleToUpdate.rack_row ||
                    bottleToUpdate.rack_row <= 0 || bottleToUpdate.rack_col <= 0)
                {
                    bottleToUpdate.rack_guid = null;
                }
            }
            var bottle = _wineData.tbl_Wine_Bottles.Where(x => x.guid == bottleToUpdate.bottle_guid).FirstOrDefault();
            if (bottle == null) bottleToUpdate.bottle_guid = "invalid";

            if (!TryValidateModel(bottleToUpdate))
            {
                return ValidationProblem(ModelState);
            }

            _wineData.tbl_Rack_Contents.Update(bottleToUpdateEntityOutput);
            _wineData.SaveChanges();
            var userBottleToReturn = _mapper.MergeInto<UserBottleDto>(bottleToUpdate, bottle);
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var mappedBottleHateoas = _mapper.Map<UserBottleDtoHateoas>(userBottleToReturn);
                mappedBottleHateoas.Links = CreateLinksForUserBottle(Guid.Parse(mappedBottleHateoas.guid)).ToList();
                return CreatedAtRoute("GetUserBottle", new { userBottleId = mappedBottleHateoas.guid }, mappedBottleHateoas);
            }
            else
                return CreatedAtRoute("GetUserBottle", new { userBottleId = userBottleToReturn.guid }, userBottleToReturn);
        }

        [HttpDelete("{userBottleId}", Name = "DeleteUserBottle")]
        public ActionResult DeleteUserBottle(Guid userBottleId, Guid ownerId)
        {
            if(ownerId == null || userBottleId == null) BadRequest("Error: guid and owner_guid are required");
            var userBottleToDelete = _wineData.tbl_Rack_Contents.Where(x => x.owner_guid == ownerId.ToString() && x.guid == userBottleId.ToString()).FirstOrDefault();
            if (userBottleToDelete == null) return NotFound();
            _wineData.tbl_Rack_Contents.Remove(userBottleToDelete);
            _wineData.SaveChanges();
            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetBottlesOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST,PATCH,DELETE");
            return Ok();
        }

        public override ActionResult ValidationProblem([ActionResultObjectValue] ModelStateDictionary modelStateDictionary)
        {
            var options = HttpContext.RequestServices
                .GetRequiredService<IOptions<ApiBehaviorOptions>>();
            return (ActionResult)options.Value.InvalidModelStateResponseFactory(ControllerContext);
        }

        private string CreateBottleResourceUri(UserBottlesResourceParameters bottlesResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetUserBottles",
                        new
                        {
                            pageNumber = bottlesResourceParameters.pageNumber - 1,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.guid,
                            bottlesResourceParameters.owner_guid,
                            bottlesResourceParameters.rack_guid,
                            bottlesResourceParameters.bottle_guid,
                            bottlesResourceParameters.rack_col,
                            bottlesResourceParameters.rack_row,
                            bottlesResourceParameters.skip_details,
                            bottlesResourceParameters.searchQuery
                        });
                case ResourceUriType.NextPage:
                    return Url.Link("GetUserBottles",
                        new
                        {
                            pageNumber = bottlesResourceParameters.pageNumber + 1,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.guid,
                            bottlesResourceParameters.owner_guid,
                            bottlesResourceParameters.rack_guid,
                            bottlesResourceParameters.bottle_guid,
                            bottlesResourceParameters.rack_col,
                            bottlesResourceParameters.rack_row,
                            bottlesResourceParameters.skip_details,
                            bottlesResourceParameters.searchQuery
                        });
                default:
                    return Url.Link("GetUserBottles",
                        new
                        {
                            bottlesResourceParameters.pageNumber,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.guid,
                            bottlesResourceParameters.owner_guid,
                            bottlesResourceParameters.rack_guid,
                            bottlesResourceParameters.bottle_guid,
                            bottlesResourceParameters.rack_col,
                            bottlesResourceParameters.rack_row,
                            bottlesResourceParameters.skip_details,
                            bottlesResourceParameters.searchQuery
                        });
            }
        }
        private IEnumerable<LinkDto> CreateLinksForUserBottle(Guid userBottleId)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(Url.Link("GetUserBottle", new { userBottleId }),
                "self",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("DeleteUserBottle", new { userBottleId }),
                "delete_user_bottle",
                "DELETE"));
            links.Add(
                new LinkDto(Url.Link("UpdateUserBottle", new { userBottleId }),
                "update_user_bottle",
                "PATCH"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForUserBottles(UserBottlesResourceParameters bottlesResourceParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(CreateBottleResourceUri(bottlesResourceParameters, ResourceUriType.Current),
                "self",
                "GET"));
            if (hasNext)
            {
                links.Add(
                    new LinkDto(CreateBottleResourceUri(bottlesResourceParameters, ResourceUriType.NextPage),
                        "nextPage",
                        "GET"));
            }
            if (hasPrevious)
            {
                links.Add(
                    new LinkDto(CreateBottleResourceUri(bottlesResourceParameters, ResourceUriType.PreviousPage),
                        "previousPage",
                        "GET"));
            }
            return links;
        }

        private double? extractNumber(string input)
        {
            if (input == null) return null;
            string output = "";
            double val = 0;
            bool hasDecimal = false;
            for (int i = 0; i < input.Length; i++)
            {
                if (Char.IsDigit(input[i]))
                    output += input[i];
                if(input[i] == '.' && !hasDecimal)
                {
                    output += input[i];
                    hasDecimal = true;
                }
            }

            if (output.Length > 0)
                val = double.Parse(output);
            return val;
        }
    }
}
