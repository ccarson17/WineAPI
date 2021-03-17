using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WineAPI.Models;

namespace WineAPI.Controllers
{
    [Route("api")]
    [ApiController]
    public class RootController : ControllerBase
    {
        [HttpGet(Name = "GetRoot")]
        public IActionResult GetRoot()
        {
            var links = new List<LinkDto>();
            links.Add(
                new LinkDto(Url.Link("GetRoot", new { }),
                "self",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("GetBottles", new { }),
                "bottles",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("CreateBottle", new { }),
                "bottles",
                "POST"));
            links.Add(
                new LinkDto(Url.Link("GetUserBottles", new { }),
                "userbottles",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("CreateUserBottle", new { }),
                "userbottles",
                "POST"));
            links.Add(
                new LinkDto(Url.Link("GetRacks", new { }),
                "racks",
                "GET"));
            links.Add(
                new LinkDto(Url.Link("CreateRack", new { }),
                "racks",
                "POST"));

            return Ok(links);
        }
    }
}
