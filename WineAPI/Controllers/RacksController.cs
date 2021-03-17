using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using WineAPI.Models;
using WineAPI.ResourceParameters;
using WineAPI.Helpers;
using Microsoft.Net.Http.Headers;

namespace WineAPI.Controllers
{
    [ApiController]
    [Route("api/racks/")]
    public class RacksController : ControllerBase
    {
        private readonly WineDataContext _wineData;
        private readonly IMapper _mapper;

        public RacksController(WineDataContext wineData, IMapper mapper)
        {
            _wineData = wineData ??
                throw new ArgumentNullException(nameof(wineData));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = "GetRacks")]
        [HttpHead]
        public IActionResult GetRacks([FromQuery] RacksResourceParameters parameters, [FromHeader(Name = "Accept")] string mediaType)
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

            if (string.IsNullOrWhiteSpace(parameters.owner_guid)) return NotFound();
            List<WineDataContext.tbl_Wine_Racks_Item> racks = new List<WineDataContext.tbl_Wine_Racks_Item>();
            var rackContents = _wineData.tbl_Rack_Contents.Join(_wineData.tbl_Wine_Bottles, rack => rack.bottle_guid, bottle => bottle.guid, (rack, bottle) => new { Rack = rack, Bottle = bottle }).Where(rackAndBottle => rackAndBottle.Rack.owner_guid == parameters.owner_guid);
            if (string.IsNullOrWhiteSpace(parameters.guid)) {
                var racksIQ = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == parameters.owner_guid).Skip(parameters.pageSize * (parameters.pageNumber - 1)).Take(parameters.pageSize);
                if (!string.IsNullOrWhiteSpace(parameters.orderBy))
                {
                    try
                    {
                        racksIQ = racksIQ.OrderBy(parameters.orderBy);
                    }
                    catch(Exception ex)
                    {
                        return BadRequest();
                    }
                }
                racks = racksIQ.ToList();
                var retrievedRacks = racks.Select(x => x.guid).ToList();
                rackContents = rackContents.Where(x => x.Rack.owner_guid == parameters.owner_guid && retrievedRacks.Contains(x.Rack.guid));
            }
            else
            {
                racks = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == parameters.owner_guid && x.guid == parameters.guid).ToList();
                rackContents = rackContents.Where(x => x.Rack.owner_guid == parameters.owner_guid && x.Rack.rack_guid == parameters.guid);
            }
            var rackmapped = _mapper.Map<List<RackDto>>(racks);
            var rackContentList = rackContents.ToList();

            if (!String.IsNullOrEmpty(parameters.rack_name))
            {
                var rackSearchResult = rackmapped.Where(x => x.rack_name == parameters.rack_name).FirstOrDefault();
                rackmapped = new List<RackDto>() { rackSearchResult };
            }

            foreach (var item in rackmapped)
            {
                var thisRacksContents = rackContentList.Where(x => x.Rack.rack_guid == item.guid).ToList();
                if(thisRacksContents.Count() > 0)
                {
                    foreach(var bottle in thisRacksContents.OrderBy(x => x.Rack.rack_row).ThenBy(x => x.Rack.rack_col))
                    {
                        var thisBottle = _mapper.Map<BottleDto>(bottle.Bottle);
                        thisBottle.guid = bottle.Rack.guid;
                        item.bottles.Add(bottle.Rack.rack_row + "," + bottle.Rack.rack_col, thisBottle);
                    }
                }
                item.bottleCount = item.bottles.Count();
            }
            if (rackmapped.Count() == 0) return NotFound();
            PagedList<RackDto> returnList = new PagedList<RackDto>(rackmapped, rackmapped.Count(), parameters.pageNumber, parameters.pageSize);

            var previousPageLink = returnList.HasPrevious ? CreateRackResourceUri(parameters, ResourceUriType.PreviousPage) : null;
            var nextPageLink = returnList.HasNext ? CreateRackResourceUri(parameters, ResourceUriType.NextPage) : null;
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

            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var rackmappedHateoas = _mapper.Map<List<RackDtoHateoas>>(rackmapped);
                foreach(var item in rackmappedHateoas)
                {
                    item.Links = CreateLinksForRack(Guid.Parse(item.guid)).ToList();
                }
                PagedList<RackDtoHateoas> returnListHateoas = new PagedList<RackDtoHateoas>(rackmappedHateoas, rackmappedHateoas.Count(), parameters.pageNumber, parameters.pageSize);
                return Ok(returnListHateoas.ToList());
            }
            else
                return Ok(returnList.ToList());
        }

        [HttpGet("{rackId}", Name = "GetRack")]
        public IActionResult GetRack(Guid rackId, [FromHeader(Name = "Accept")] string mediaType)
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

            string rackIdStr = "";
            if (rackId != null) rackIdStr = rackId.ToString();
            var rackFromDB = _wineData.tbl_Wine_Racks.Where(x => x.guid == rackIdStr).FirstOrDefault();
            if (rackFromDB == null)
                return NotFound();
            var rackOut = _mapper.Map<RackDto>(rackFromDB);
            var rackContents = _wineData.tbl_Rack_Contents.Join(_wineData.tbl_Wine_Bottles, rack => rack.bottle_guid, bottle => bottle.guid, (rack, bottle) => new { Rack = rack, Bottle = bottle }).Where(rackAndBottle => rackAndBottle.Rack.rack_guid == rackId.ToString()).ToList();
            if (rackContents.Count() > 0)
            {
                foreach (var bottle in rackContents.OrderBy(x => x.Rack.rack_row).ThenBy(x => x.Rack.rack_col))
                {
                    var thisBottle = _mapper.Map<BottleDto>(bottle.Bottle);
                    thisBottle.guid = bottle.Rack.guid;
                    rackOut.bottles.Add(bottle.Rack.rack_row + "," + bottle.Rack.rack_col, thisBottle);
                }
            }
            rackOut.bottleCount = rackOut.bottles.Count();
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var rackOutHateoas = _mapper.Map<RackDtoHateoas>(rackOut);
                rackOutHateoas.Links = CreateLinksForRack(Guid.Parse(rackOutHateoas.guid)).ToList();
                return Ok(rackOutHateoas);
            }
            else
                return Ok(rackOut);
        }

        [HttpPost(Name = "CreateRack")]
        public ActionResult<RackDto> CreateRack(RackForCreationDto rack, [FromHeader(Name = "Accept")] string mediaType)
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

            if (rack.owner_guid == null || rack.rows == 0 || rack.cols == 0)
                return BadRequest("Error: owner_guid, rows and cols must be non-zero and not null");
            var rackEntity = _mapper.Map<WineDataContext.tbl_Wine_Racks_Item>(rack);
            rackEntity.guid = Guid.NewGuid().ToString();
            _wineData.tbl_Wine_Racks.Add(rackEntity);
            _wineData.SaveChanges();
            var rackToReturn = _mapper.Map<RackDto>(rackEntity);
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var rackOutHateoas = _mapper.Map<RackDtoHateoas>(rackToReturn);
                rackOutHateoas.Links = CreateLinksForRack(Guid.Parse(rackOutHateoas.guid)).ToList();
                return CreatedAtRoute("GetRack", new { rackId = rackOutHateoas.guid }, rackOutHateoas);
            }
            else
                return CreatedAtRoute("GetRack", new { rackId = rackToReturn.guid }, rackToReturn);
        }

        [HttpPatch("{rackId}", Name = "UpdateRack")]
        public ActionResult<RackDto> UpdateRack(Guid rackId, Guid ownerId, JsonPatchDocument<RackForUpdateDto> patchDocument, [FromHeader(Name = "Accept")] string mediaType)
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

            if (rackId == null || ownerId == null)
                return BadRequest("Error: rackId and ownerId are required");

            var rackToUpdateEntity = _wineData.tbl_Wine_Racks.Where(x => x.guid == rackId.ToString() && x.owner_guid == ownerId.ToString()).FirstOrDefault();
            string oldRackName = "";
            if (rackToUpdateEntity == null)
                return BadRequest("RackId not found: " + rackId);
            else
                oldRackName = rackToUpdateEntity.rack_name;
            var rackToUpdate = _mapper.Map<RackForUpdateDto>(rackToUpdateEntity);
            _wineData.Entry(rackToUpdateEntity).State = EntityState.Detached;
            patchDocument.ApplyTo(rackToUpdate);
            var rackToUpdateEntityOutput = _mapper.Map<WineDataContext.tbl_Wine_Racks_Item>(rackToUpdate);
            if(rackToUpdateEntityOutput.guid != rackId.ToString()) return BadRequest("Cannot change rackId");
            var rackContents = _wineData.tbl_Rack_Contents.Where(x => x.rack_guid == rackId.ToString()).ToList();
            rackToUpdate.bottleCount = rackContents.Count();

            if (oldRackName != rackToUpdateEntityOutput.rack_name)
            {
                // update all bottles in this rack to the new rack name
                foreach (var bottle in rackContents)
                {
                    bottle.rack_name = rackToUpdateEntityOutput.rack_name;
                }
            }

            var orphanCount = rackContents.Where(x => x.rack_row > rackToUpdateEntityOutput.rows || x.rack_col > rackToUpdateEntityOutput.cols).Count();
            if(orphanCount > 0)
            {
                var orphans = rackContents.Where(x => x.rack_row > rackToUpdateEntityOutput.rows || x.rack_col > rackToUpdateEntityOutput.cols);
                foreach(var item in orphans)
                {
                    item.rack_row = 0;
                    item.rack_col = 0;
                    item.rack_guid = null;
                    item.rack_name = null;
                }
            }
            //if(orphanCount > 0) return BadRequest("Error: " + orphanCount + " bottles would not fit after rack update. Please move these bottles and try again.");

            _wineData.tbl_Wine_Racks.Update(rackToUpdateEntityOutput);
            _wineData.SaveChanges();
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var rackOutHateoas = _mapper.Map<RackForUpdateDtoHateoas>(rackToUpdate);
                rackOutHateoas.Links = CreateLinksForRack(Guid.Parse(rackOutHateoas.guid)).ToList();
                return CreatedAtRoute("GetRack", new { rackId = rackOutHateoas.guid }, rackOutHateoas);
            }
            else
                return CreatedAtRoute("GetRack", new { rackId = rackToUpdate.guid }, rackToUpdate);
        }

        [HttpDelete("{rackId}", Name = "DeleteRack")]
        public ActionResult DeleteRack(Guid ownerId, Guid rackId)
        {
            if (ownerId == null || rackId == null) BadRequest("Error: guid and owner_guid are required");
            var rackToDelete = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == ownerId.ToString() && x.guid == rackId.ToString()).FirstOrDefault();
            if (rackToDelete == null) return NotFound();
            _wineData.tbl_Wine_Racks.Remove(rackToDelete);
            _wineData.SaveChanges();
            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetRacksOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST,PATCH,DELETE");
            return Ok();
        }

        private string CreateRackResourceUri(RacksResourceParameters racksResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetUserBottle",
                        new
                        {
                            pageNumber = racksResourceParameters.pageNumber - 1,
                            racksResourceParameters.pageSize,
                            racksResourceParameters.orderBy,
                            racksResourceParameters.guid,
                            racksResourceParameters.owner_guid,
                            racksResourceParameters.rows,
                            racksResourceParameters.cols
                        });
                case ResourceUriType.NextPage:
                    return Url.Link("GetUserBottle",
                        new
                        {
                            pageNumber = racksResourceParameters.pageNumber + 1,
                            racksResourceParameters.pageSize,
                            racksResourceParameters.orderBy,
                            racksResourceParameters.guid,
                            racksResourceParameters.owner_guid,
                            racksResourceParameters.rows,
                            racksResourceParameters.cols
                        });
                default:
                    return Url.Link("GetUserBottle",
                        new
                        {
                            racksResourceParameters.pageNumber,
                            racksResourceParameters.pageSize,
                            racksResourceParameters.orderBy,
                            racksResourceParameters.guid,
                            racksResourceParameters.owner_guid,
                            racksResourceParameters.rows,
                            racksResourceParameters.cols
                        });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForRack(Guid rackId)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(Url.Link("GetRack", new { rackId }),
                "self",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("DeleteRack", new { rackId }),
                "delete_rack",
                "DELETE"));
            links.Add(
                new LinkDto(Url.Link("UpdateRack", new { rackId }),
                "update_rack",
                "PATCH"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForRacks(RacksResourceParameters racksResourceParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(CreateRackResourceUri(racksResourceParameters, ResourceUriType.Current),
                "self",
                "GET"));
            if (hasNext)
            {
                links.Add(
                    new LinkDto(CreateRackResourceUri(racksResourceParameters, ResourceUriType.NextPage),
                        "nextPage",
                        "GET"));
            }
            if (hasPrevious)
            {
                links.Add(
                    new LinkDto(CreateRackResourceUri(racksResourceParameters, ResourceUriType.PreviousPage),
                        "previousPage",
                        "GET"));
            }
            return links;
        }
    }
}
