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
    [Route("api/userbottlesez/")]
    public class UserBottlesEzController : Controller
    {
        private readonly WineDataContext _wineData;
        private readonly IMapper _mapper;
        public UserBottlesEzController(WineDataContext wineData, IMapper mapper)
        {
            _wineData = wineData ??
                throw new ArgumentNullException(nameof(wineData));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
        }


        // this function allows a user bottle to be created by providing all needed info
        // the process finds a current bottle instance, updates it as needed, then creates a user bottle using that bottle
        // if no bottle is found, a new one is created automatically
        [HttpPost(Name = "CreateUserBottleEz")]
        public ActionResult<UserBottleDto> CreateUserBottleEz(UserBottleForEzCreationDto userbottle, [FromHeader(Name = "Accept")] string mediaType)
        {
            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }

            var bottle = _wineData.tbl_Wine_Bottles.Where(x => 
                x.Vintner.ToLower().Trim() == userbottle.Vintner.ToLower().Trim() &&
                x.Varietal.ToLower().Trim() == userbottle.Varietal.ToLower().Trim() &&
                x.WineName.ToLower().Trim() == userbottle.WineName.ToLower().Trim() &&
                x.Year == userbottle.Year &&
                x.Category.ToLower().Trim() == userbottle.Category.ToLower().Trim() &&
                x.SizeInML == userbottle.SizeInML &&
                (x.owner_guid == userbottle.owner_guid || x.owner_guid == null)
            ).FirstOrDefault();
            if(bottle != null)
            {
                // update bottle
                // allow these fields to update to account for case changes (since they were matched above already, that is the only possible difference).
                bottle.Vintner = userbottle.Vintner ?? bottle.Vintner;
                bottle.Varietal = userbottle.Varietal ?? bottle.Varietal;
                bottle.WineName = userbottle.WineName ?? bottle.WineName;
                bottle.Year = userbottle.Year ?? bottle.Year;
                bottle.Category = userbottle.Category ?? bottle.Category;
                // update the rest of the bottle, taking the new value first and the old only if the new is null
                bottle.owner_guid = userbottle.owner_guid;
                bottle.City_Town = userbottle.City_Town ?? bottle.City_Town;
                bottle.Region = userbottle.Region ?? bottle.Region;
                bottle.State_Province = userbottle.State_Province ?? bottle.State_Province;
                bottle.Country = userbottle.Country ?? bottle.Country;
                bottle.ExpertRatings = userbottle.ExpertRatings ?? bottle.ExpertRatings;
                bottle.SizeInML = userbottle.SizeInML != 0 ? userbottle.SizeInML : bottle.SizeInML;
                bottle.ABV = userbottle.ABV ?? bottle.ABV;
                bottle.WinemakerNotes = userbottle.WinemakerNotes ?? bottle.WinemakerNotes;
                _wineData.tbl_Wine_Bottles.Update(bottle);
                _wineData.SaveChanges();
            }
            else
            {
                // create new bottle
                bottle = new WineDataContext.tbl_Wine_Bottles_Item()
                {
                    guid = Guid.NewGuid().ToString(),
                    owner_guid = userbottle.owner_guid,
                    Year = userbottle.Year,
                    Vintner = userbottle.Vintner,
                    WineName = userbottle.WineName,
                    Category = userbottle.Category,
                    Varietal = userbottle.Varietal,
                    City_Town = userbottle.City_Town,
                    Region = userbottle.Region,
                    State_Province = userbottle.State_Province,
                    Country = userbottle.Country,
                    ExpertRatings = userbottle.ExpertRatings,
                    SizeInML = userbottle.SizeInML,
                    ABV = userbottle.ABV,
                    WinemakerNotes = userbottle.WinemakerNotes
                };
                _wineData.tbl_Wine_Bottles.Add(bottle);
                _wineData.SaveChanges();
            }
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
            else
            {
                if(!string.IsNullOrEmpty(userbottle.rack_name))
                {
                    var existingRack = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == userbottle.owner_guid && x.rack_name == userbottle.rack_name).FirstOrDefault();
                    userBottleEntity.rack_guid = existingRack.guid;
                }
                else { userbottle.rack_name = null; }
            }
            userBottleEntity.created_date = DateTime.UtcNow;
            userBottleEntity.bottle_guid = bottle.guid;
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

        [HttpPatch("{userBottleId}", Name = "UpdateUserBottleEz")]
        public ActionResult<UserBottleDto> UpdateUserBottleEz(Guid userBottleId, Guid ownerId, JsonPatchDocument<UserBottleForEzCreationDto> patchDocument, [FromHeader(Name = "Accept")] string mediaType)
        {
            MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType);
            if (parsedMediaType == null)
            {
                MediaTypeHeaderValue.TryParse("application/json", out parsedMediaType);
            }
            if (userBottleId == null || ownerId == null)
                return BadRequest("Error: guid and owner_guid are required");

            var userBottleToUpdateEntity = _wineData.tbl_Rack_Contents.Where(x => x.guid == userBottleId.ToString() && x.owner_guid == ownerId.ToString()).FirstOrDefault();
            if (userBottleToUpdateEntity == null)
                return BadRequest("Error: User Bottle Not Found");
            var bottleToUpdateEntity = _wineData.tbl_Wine_Bottles.Where(x => x.guid == userBottleToUpdateEntity.bottle_guid && (x.owner_guid == ownerId.ToString() || x.owner_guid == null)).FirstOrDefault();
            if (bottleToUpdateEntity == null)
                return BadRequest("Error: Bottle Not Found");

            UserBottleForEzCreationDto bottleForCreation = new UserBottleForEzCreationDto()
            {
                owner_guid = userBottleToUpdateEntity.owner_guid,
                rack_guid = userBottleToUpdateEntity.rack_guid,
                bottle_guid = userBottleToUpdateEntity.bottle_guid,
                rack_name = userBottleToUpdateEntity.rack_name,
                rack_col = userBottleToUpdateEntity.rack_col,
                rack_row = userBottleToUpdateEntity.rack_row,
                where_bought = userBottleToUpdateEntity.where_bought,
                price_paid = userBottleToUpdateEntity.price_paid,
                user_rating = userBottleToUpdateEntity.user_rating,
                user_notes = userBottleToUpdateEntity.user_notes,
                Year = bottleToUpdateEntity.Year,
                Vintner = bottleToUpdateEntity.Vintner,
                WineName = bottleToUpdateEntity.WineName,
                Category = bottleToUpdateEntity.Category,
                Varietal = bottleToUpdateEntity.Varietal,
                City_Town = bottleToUpdateEntity.City_Town,
                Region = bottleToUpdateEntity.Region,
                State_Province = bottleToUpdateEntity.State_Province,
                Country = bottleToUpdateEntity.Country,
                ExpertRatings = bottleToUpdateEntity.ExpertRatings,
                SizeInML = bottleToUpdateEntity.SizeInML,
                ABV = bottleToUpdateEntity.ABV,
                WinemakerNotes = bottleToUpdateEntity.WinemakerNotes
            };

            patchDocument.ApplyTo(bottleForCreation);

            //_wineData.Entry(userBottleToUpdateEntity).State = EntityState.Detached;

            userBottleToUpdateEntity.owner_guid = bottleForCreation.owner_guid;
            userBottleToUpdateEntity.rack_guid = bottleForCreation.rack_guid;
            userBottleToUpdateEntity.bottle_guid = bottleForCreation.bottle_guid;
            userBottleToUpdateEntity.rack_name = bottleForCreation.rack_name;
            userBottleToUpdateEntity.rack_col = bottleForCreation.rack_col;
            userBottleToUpdateEntity.rack_row = bottleForCreation.rack_row;
            userBottleToUpdateEntity.where_bought = bottleForCreation.where_bought;
            userBottleToUpdateEntity.price_paid = bottleForCreation.price_paid;
            userBottleToUpdateEntity.user_rating = bottleForCreation.user_rating;
            userBottleToUpdateEntity.user_notes = bottleForCreation.user_notes;

            userBottleToUpdateEntity = verifyRackInfo(userBottleToUpdateEntity);

            if (!TryValidateModel(userBottleToUpdateEntity))
            {
                return ValidationProblem(ModelState);
            }

            var returnBottle = new WineDataContext.tbl_Wine_Bottles_Item();
            var bottleInstances = _wineData.tbl_Rack_Contents.Where(x => x.bottle_guid == userBottleToUpdateEntity.bottle_guid).ToList();
            if (bottleInstances.Count() == 1)
            {
                //_wineData.Entry(bottleToUpdateEntity).State = EntityState.Detached;
                bottleToUpdateEntity.Year = bottleForCreation.Year;
                bottleToUpdateEntity.Vintner = bottleForCreation.Vintner;
                bottleToUpdateEntity.WineName = bottleForCreation.WineName;
                bottleToUpdateEntity.Category = bottleForCreation.Category;
                bottleToUpdateEntity.Varietal = bottleForCreation.Varietal;
                bottleToUpdateEntity.City_Town = bottleForCreation.City_Town;
                bottleToUpdateEntity.Region = bottleForCreation.Region;
                bottleToUpdateEntity.State_Province = bottleForCreation.State_Province;
                bottleToUpdateEntity.Country = bottleForCreation.Country;
                bottleToUpdateEntity.ExpertRatings = bottleForCreation.ExpertRatings;
                bottleToUpdateEntity.SizeInML = bottleForCreation.SizeInML;
                bottleToUpdateEntity.ABV = bottleForCreation.ABV;
                bottleToUpdateEntity.WinemakerNotes = bottleForCreation.WinemakerNotes;

                if (!TryValidateModel(bottleToUpdateEntity))
                {
                    return ValidationProblem(ModelState);
                }
                returnBottle = bottleToUpdateEntity;
                _wineData.tbl_Wine_Bottles.Update(bottleToUpdateEntity);
            }
            else
            {
                var newBottleToAdd = new WineDataContext.tbl_Wine_Bottles_Item()
                {
                    guid = Guid.NewGuid().ToString(),
                    owner_guid = bottleForCreation.owner_guid,
                    Year = bottleForCreation.Year,
                    Vintner = bottleForCreation.Vintner,
                    WineName = bottleForCreation.WineName,
                    Category = bottleForCreation.Category,
                    Varietal = bottleForCreation.Varietal,
                    City_Town = bottleForCreation.City_Town,
                    Region = bottleForCreation.Region,
                    State_Province = bottleForCreation.State_Province,
                    Country = bottleForCreation.Country,
                    ExpertRatings = bottleForCreation.ExpertRatings,
                    SizeInML = bottleForCreation.SizeInML,
                    ABV = bottleForCreation.ABV,
                    WinemakerNotes = bottleForCreation.WinemakerNotes
                };

                if (!TryValidateModel(newBottleToAdd))
                {
                    return ValidationProblem(ModelState);
                }
                returnBottle = newBottleToAdd;
                userBottleToUpdateEntity.bottle_guid = returnBottle.guid;
                _wineData.tbl_Wine_Bottles.Add(newBottleToAdd);
            }
            _wineData.tbl_Rack_Contents.Update(userBottleToUpdateEntity);
            _wineData.SaveChanges();

            UserBottleDto userBottleToReturn = _mapper.MergeInto<UserBottleDto>(userBottleToUpdateEntity, returnBottle);
            if (parsedMediaType.MediaType == "application/vnd.pe7.hateoas+json")
            {
                var mappedBottleHateoas = _mapper.Map<UserBottleDtoHateoas>(userBottleToReturn);
                mappedBottleHateoas.Links = CreateLinksForUserBottle(Guid.Parse(mappedBottleHateoas.guid)).ToList();
                return CreatedAtRoute("GetUserBottle", new { userBottleId = mappedBottleHateoas.guid }, mappedBottleHateoas);
            }
            else
                return CreatedAtRoute("GetUserBottle", new { userBottleId = userBottleToReturn.guid }, userBottleToReturn);
        }

        // checks if requested rack slot is filled and removes rack info if so (making the new bottle "unassigned" to a rack location
        private WineDataContext.tbl_Rack_Contents_Item verifyRackInfo(WineDataContext.tbl_Rack_Contents_Item userBottle)
        {
            var rack = new WineDataContext.tbl_Wine_Racks_Item();
            if (userBottle.rack_guid != null)
            {
                rack = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == userBottle.owner_guid
                    && x.guid == userBottle.rack_guid
                    && x.rack_name == userBottle.rack_name).FirstOrDefault();
            }
            else if (!String.IsNullOrEmpty(userBottle.rack_name))
            {
                rack = _wineData.tbl_Wine_Racks.Where(x => x.owner_guid == userBottle.owner_guid
                    && x.rack_name == userBottle.rack_name).FirstOrDefault();
                userBottle.rack_guid = rack.guid;
            }
            else return userBottle;
            // check if rack slot is occupied
            var rackContents = _wineData.tbl_Rack_Contents.Where(x => x.rack_guid == rack.guid
                && x.rack_row == userBottle.rack_row
                && x.rack_col == userBottle.rack_col).FirstOrDefault();
            if (rackContents == null || rackContents.guid == userBottle.guid)
            {
                // check if rack location exists
                if(userBottle.rack_row > rack.rows || userBottle.rack_col > rack.cols)
                {
                    // location doesn't exist, zero out rack info
                    userBottle.rack_guid = null;
                    userBottle.rack_row = 0;
                    userBottle.rack_col = 0;
                    userBottle.rack_name = null;
                }
                return userBottle;
            }
            else
            {
                userBottle.rack_guid = null;
                userBottle.rack_row = 0;
                userBottle.rack_col = 0;
                userBottle.rack_name = null;
                return userBottle;
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
    }
}
