using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using WineAPI.Helpers;
using WineAPI.Models;
using WineAPI.ResourceParameters;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using Org.BouncyCastle.Crypto;

namespace WineAPI.Controllers
{
    [ApiController]
    [Route("api/bottles/")]
   
    public class BottlesController : ControllerBase
    {
        private readonly WineDataContext _wineData;
        private readonly IMapper _mapper;

        public BottlesController(WineDataContext wineData, IMapper mapper)
        {
            _wineData = wineData ??
                throw new ArgumentNullException(nameof(wineData));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet(Name = "GetBottles")]
        [HttpHead]
        //[ResponseCache(Duration = 120)]
        public IActionResult GetBottles([FromQuery] BottlesResourceParameters parameters, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            //{
            //    return BadRequest();
            //}

            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if(parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            //Response.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
            //Response.Headers[HeaderNames.Expires] = "0";
            //Response.Headers[HeaderNames.Pragma] = "no-cache";

            var json = JsonConvert.SerializeObject(parameters);
            var filters = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            if (filters.Count() == 0) {
                if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
                {
                    var returnData = _mapper.Map<List<BottleDtoHateoas>>(_wineData.tbl_Wine_Bottles.Skip(parameters.pageSize * (parameters.pageNumber - 1)).Take(parameters.pageSize).ToList());
                    foreach (var item in returnData)
                    {
                        item.Links = CreateLinksForBottle(Guid.Parse(item.guid)).ToList();
                    }
                    return Ok(returnData);
                }
                else
                    return Ok(_mapper.Map<List<BottleDto>>(_wineData.tbl_Wine_Bottles.Skip(parameters.pageSize * (parameters.pageNumber - 1)).Take(parameters.pageSize).ToList()));
            }
            var collection = _wineData.tbl_Wine_Bottles as IQueryable<WineDataContext.tbl_Wine_Bottles_Item>;
            foreach (KeyValuePair<string, string> item in filters)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                {
                    if (item.Key.ToLower() == "year" || item.Key.ToLower() == "size" || item.Key.ToLower() == "abv") collection = filterCollectionNumeric(collection, item.Key, item.Value);
                    else if (item.Key.ToLower() != "searchquery" && item.Key.ToLower() != "pagesize" && item.Key.ToLower() != "pagenumber" && item.Key.ToLower() != "orderby") collection = filterCollectionText(collection, item.Key, item.Value);
                }
            }
            if (filters.ContainsKey("searchQuery"))
            {
                if (!string.IsNullOrWhiteSpace(filters["searchQuery"]))
                {
                    string searchQuery = filters["searchQuery"].Trim().ToLower();
                    int searchInt = 0;
                    Decimal searchDec = 0;
                    int.TryParse(searchQuery, out searchInt);
                    Decimal.TryParse(searchQuery, out searchDec);
                    collection = collection.Where(x =>
                        (searchInt != 0 && x.Year == searchInt) ||
                        x.Vintner.ToLower().IndexOf(searchQuery) != -1 ||
                        x.WineName.ToLower().IndexOf(searchQuery) != -1 ||
                        x.Category.ToLower().IndexOf(searchQuery) != -1 ||
                        x.Varietal.ToLower().IndexOf(searchQuery) != -1 ||
                        x.City_Town.ToLower().IndexOf(searchQuery) != -1 ||
                        x.Region.ToLower().IndexOf(searchQuery) != -1 ||
                        x.State_Province.ToLower().IndexOf(searchQuery) != -1 ||
                        x.Country.ToLower().IndexOf(searchQuery) != -1 ||
                        x.ExpertRatings.ToLower().IndexOf(searchQuery) != -1 ||
                        (searchInt != 0 && x.SizeInML == searchInt) ||
                        (searchDec != 0 && x.ABV == searchDec) ||
                        x.WinemakerNotes.ToLower().IndexOf(searchQuery) != -1
                    );
                }
            }
            if(!string.IsNullOrWhiteSpace(parameters.orderBy))
            {
                try
                {
                    string modifiedOrderBy = parameters.orderBy.ToLower().Replace("size", "sizeinml");
                    collection = collection.OrderBy(parameters.orderBy);
                }
                catch
                {
                    return BadRequest();
                }
            }
            PagedList<WineDataContext.tbl_Wine_Bottles_Item> returnList = PagedList<WineDataContext.tbl_Wine_Bottles_Item>.Create(collection, parameters.pageNumber, parameters.pageSize);
            var paginationMetadata = new
            {
                totalCount = returnList.TotalCount,
                pageSize = returnList.PageSize,
                currentPage = returnList.CurrentPage,
                totalPages = returnList.TotalPages,
                parameters.orderBy
            };
            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(paginationMetadata));

            var links = CreateLinksForBottles(parameters, returnList.HasNext, returnList.HasPrevious);

            if (returnList.Count() > 0)
            {
                if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
                {
                    var mappedBottles = _mapper.Map<List<BottleDtoHateoas>>(returnList.ToList());
                    foreach (var item in mappedBottles)
                    {
                        item.Links = CreateLinksForBottle(Guid.Parse(item.guid)).ToList();
                    }
                    BottleCollection bottleCollection = new BottleCollection(mappedBottles, links.ToList());
                    return Ok(bottleCollection);
                }
                else
                {
                    return Ok(_mapper.Map<List<BottleDto>>(returnList.ToList()));
                }
            }
            else
                return NotFound();
        }

        [HttpGet("{bottleId}", Name = "GetBottle")]
        public IActionResult GetBottle(Guid bottleId, [FromHeader(Name = "Accept")] string mediaType)
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

            string bottleIdStr = "";
            if (bottleId != null) bottleIdStr = bottleId.ToString();
            var wineFromDB = _wineData.tbl_Wine_Bottles.Where(x => x.guid == bottleIdStr).FirstOrDefault();
            if (wineFromDB != null)
            {
                if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
                {
                    var bottleToReturn = _mapper.Map<BottleDtoHateoas>(wineFromDB);
                    bottleToReturn.Links = CreateLinksForBottle(bottleId).ToList();
                    return Ok(bottleToReturn);
                }
                else
                {
                    var bottleToReturn = _mapper.Map<BottleDto>(wineFromDB);
                    return Ok(bottleToReturn);
                }
            }
            else return NotFound();
        }

        [HttpPost(Name = "CreateBottle")]
        public ActionResult<BottleDto> CreateBottle(BottleForCreationDto bottle, [FromHeader(Name = "Accept")] string mediaType) 
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

            var bottleEntity = _mapper.Map<WineDataContext.tbl_Wine_Bottles_Item>(bottle);
            //var existingBottles = _wineData.tbl_Wine_Bottles.Where(x => x.Year == bottleEntity.Year && x.Vintner == bottleEntity.Vintner && x.WineName == bottleEntity.WineName &&
            //    x.Category == bottleEntity.Category && x.Varietal == bottleEntity.Varietal && x.SizeInML == bottleEntity.SizeInML).ToList();
            //if (existingBottles.Count() > 0) 
            //    return Conflict("Bottle already exists: guid = " + existingBottles.Select(x => x.guid).First().ToString());
            bottleEntity.guid = Guid.NewGuid().ToString();
            _wineData.tbl_Wine_Bottles.Add(bottleEntity);
            _wineData.SaveChanges();
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var bottleToReturn = _mapper.Map<BottleDtoHateoas>(bottleEntity);
                bottleToReturn.Links = CreateLinksForBottle(Guid.Parse(bottleToReturn.guid)).ToList();
                return CreatedAtRoute("GetBottle", new { bottleId = bottleToReturn.guid }, bottleToReturn);
            }
            else
            {
                var bottleToReturn = _mapper.Map<BottleDto>(bottleEntity);
                return CreatedAtRoute("GetBottle", new { bottleId = bottleToReturn.guid }, bottleToReturn);
            }
        }

        [HttpPatch("{bottleId}", Name = "UpdateBottle")]
        public ActionResult<UserBottleDto> UpdateBottle(Guid bottleId, JsonPatchDocument<BottleForUpdateDto> patchDocument, [FromHeader(Name = "Accept")] string mediaType)
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

            if (bottleId == null)
                return BadRequest("Error: guid is required");
            var bottleToUpdateEntity = _wineData.tbl_Wine_Bottles.Where(x => x.guid == bottleId.ToString()).FirstOrDefault();
            if (bottleToUpdateEntity == null)
                return BadRequest("Bottle guid not found: " + bottleId);
            var bottleToUpdate = _mapper.Map<BottleForUpdateDto>(bottleToUpdateEntity);
            _wineData.Entry(bottleToUpdateEntity).State = EntityState.Detached;
            patchDocument.ApplyTo(bottleToUpdate);
            var bottleToUpdateEntityOutput = _mapper.Map<WineDataContext.tbl_Wine_Bottles_Item>(bottleToUpdate);
            if (!TryValidateModel(bottleToUpdate))
            {
                return ValidationProblem(ModelState);
            }
            _wineData.tbl_Wine_Bottles.Update(bottleToUpdateEntityOutput);
            _wineData.SaveChanges();
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var bottleToUpdateHateoas = _mapper.Map<BottleForUpdateDtoHateoas>(bottleToUpdate);
                bottleToUpdateHateoas.Links = CreateLinksForBottle(Guid.Parse(bottleToUpdate.guid)).ToList();
                return CreatedAtRoute("GetBottle", new { bottleId = bottleToUpdateHateoas.guid }, bottleToUpdateHateoas);
            }
            else
            {
                return CreatedAtRoute("GetBottle", new { bottleId = bottleToUpdate.guid }, bottleToUpdate);
            }
        }

        [HttpDelete("{bottleId}", Name = "DeleteBottle")]
        public ActionResult DeleteUserBottle(Guid bottleId)
        {
            if (bottleId == null) BadRequest("Error: guid and is required");
            var bottleToDelete = _wineData.tbl_Wine_Bottles.Where(x => x.guid == bottleId.ToString()).FirstOrDefault();
            if (bottleToDelete == null) return NotFound();
            _wineData.tbl_Wine_Bottles.Remove(bottleToDelete);
            _wineData.SaveChanges();
            return NoContent();
        }

        [HttpOptions]
        public IActionResult GetBottlesOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST,PATCH,DELETE");
            return Ok();
        }

        private IQueryable<WineDataContext.tbl_Wine_Bottles_Item> filterCollectionNumeric(IQueryable<WineDataContext.tbl_Wine_Bottles_Item> collection,
            string fieldName, string filterText)
        {
            int filterIntMin = 0;
            int filterIntMax = 0;
            Decimal filterDecMin = 0;
            Decimal filterDecMax = 0;
            filterText = filterText.Trim().ToLower();
            string[] textSplit = filterText.Split(":");
            string filterType = "";
            if (textSplit.Count() == 1)
            {
                int.TryParse(textSplit[0], out filterIntMin);
                Decimal.TryParse(textSplit[0], out filterDecMin);
            }
            else if (textSplit.Count() == 2)
            {
                int.TryParse(textSplit[1], out filterIntMin);
                Decimal.TryParse(textSplit[1], out filterDecMin);
                filterType = textSplit[0];
            }
            else if (textSplit.Count() == 3)
            {
                int.TryParse(textSplit[1], out filterIntMin);
                Decimal.TryParse(textSplit[1], out filterDecMin);
                int.TryParse(textSplit[2], out filterIntMax);
                Decimal.TryParse(textSplit[2], out filterDecMax);
                filterType = textSplit[0];
            }
            else return collection;
            if (filterIntMin != 0)
            {
                if (filterType == "" || filterType == "equals")
                    return collection.Where(fieldName + " == " + filterIntMin);
                else if (filterType == "max")
                    return collection.Where(fieldName + " <= " + filterIntMin);
                else if (filterType == "min")
                    return collection.Where(fieldName + " >= " + filterIntMin);
                else if (filterType == "between")
                    return collection.Where(fieldName + " >= " + filterIntMin + " AND " + fieldName + " <= " + filterIntMax);
            }
            else if(filterDecMin != 0)
            {
                if (filterType == "" || filterType == "equals")
                    return collection.Where(fieldName + " == " + filterDecMin);
                else if (filterType == "max")
                    return collection.Where(fieldName + " <= " + filterDecMin);
                else if (filterType == "min")
                    return collection.Where(fieldName + " >= " + filterDecMin);
                else if (filterType == "between")
                    return collection.Where(fieldName + " >= " + filterDecMin + " AND " + fieldName + " <= " + filterDecMax);
            }

            return collection.Take(0);
        }

        private IQueryable<WineDataContext.tbl_Wine_Bottles_Item> filterCollectionText(IQueryable<WineDataContext.tbl_Wine_Bottles_Item> collection,
                        string fieldName, string filterText)
        {
            filterText = filterText.Trim().ToLower();
            try
            {
                collection = collection.Where(fieldName + " == \"" + filterText + "\"");
            }
            catch
            {
                return collection.Take(0);
            }
            return collection;
        }

        private string CreateBottleResourceUri(BottlesResourceParameters bottlesResourceParameters, ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetBottles",
                        new
                        {
                            pageNumber = bottlesResourceParameters.pageNumber - 1,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.Year,
                            bottlesResourceParameters.Vintner,
                            bottlesResourceParameters.WineName,
                            bottlesResourceParameters.Category,
                            bottlesResourceParameters.Varietal,
                            bottlesResourceParameters.City_Town,
                            bottlesResourceParameters.Region,
                            bottlesResourceParameters.State_Province,
                            bottlesResourceParameters.Country,
                            bottlesResourceParameters.ExpertRatings,
                            bottlesResourceParameters.Size,
                            bottlesResourceParameters.ABV,
                            bottlesResourceParameters.WinemakerNotes,
                            bottlesResourceParameters.searchQuery
                        });
                case ResourceUriType.NextPage:
                    return Url.Link("GetBottles",
                        new
                        {
                            pageNumber = bottlesResourceParameters.pageNumber + 1,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.Year,
                            bottlesResourceParameters.Vintner,
                            bottlesResourceParameters.WineName,
                            bottlesResourceParameters.Category,
                            bottlesResourceParameters.Varietal,
                            bottlesResourceParameters.City_Town,
                            bottlesResourceParameters.Region,
                            bottlesResourceParameters.State_Province,
                            bottlesResourceParameters.Country,
                            bottlesResourceParameters.ExpertRatings,
                            bottlesResourceParameters.Size,
                            bottlesResourceParameters.ABV,
                            bottlesResourceParameters.WinemakerNotes,
                            bottlesResourceParameters.searchQuery
                        });
                default:
                    return Url.Link("GetBottles",
                        new
                        {
                            bottlesResourceParameters.pageNumber,
                            bottlesResourceParameters.pageSize,
                            bottlesResourceParameters.orderBy,
                            bottlesResourceParameters.Year,
                            bottlesResourceParameters.Vintner,
                            bottlesResourceParameters.WineName,
                            bottlesResourceParameters.Category,
                            bottlesResourceParameters.Varietal,
                            bottlesResourceParameters.City_Town,
                            bottlesResourceParameters.Region,
                            bottlesResourceParameters.State_Province,
                            bottlesResourceParameters.Country,
                            bottlesResourceParameters.ExpertRatings,
                            bottlesResourceParameters.Size,
                            bottlesResourceParameters.ABV,
                            bottlesResourceParameters.WinemakerNotes,
                            bottlesResourceParameters.searchQuery
                        });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForBottle(Guid bottleId)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(Url.Link("GetBottle", new { bottleId }),
                "self",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("DeleteBottle", new { bottleId }),
                "delete_bottle",
                "DELETE"));
            links.Add(
                new LinkDto(Url.Link("UpdateBottle", new { bottleId }),
                "update_bottle",
                "PATCH"));
            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForBottles(BottlesResourceParameters bottlesResourceParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(CreateBottleResourceUri(bottlesResourceParameters, ResourceUriType.Current),
                "self",
                "GET"));
            if(hasNext)
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
    }
}
