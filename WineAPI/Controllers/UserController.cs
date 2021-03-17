using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using WineAPI.Models;

namespace WineAPI.Controllers
{
    [ApiController]
    [Route("api/users/")]
    public class UserController : ControllerBase
    {
        private readonly WineDataContext _wineData;

        public UserController(WineDataContext wineData)
        {
            _wineData = wineData ??
                throw new ArgumentNullException(nameof(wineData));
        }

        [HttpGet("{uid}", Name = "GetUserProfile")]
        public IActionResult GetUserProfile(string uid)
        {
            var apiKey = _wineData.lk_Okta_API_Key.Select(x => x.API_Key).FirstOrDefault();
            if (apiKey == null) return NotFound();
            var userProfile = new UserDto();
            var client = new RestClient("https://dev-364313.okta.com/api/v1/apps/0oa138hfqseIm0EpE4x7/users/" + uid);
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            //request.AddHeader("Authorization", "SSWS 00N1sNpyFu0bXooEYaXEzNvkqQvozi0H-vANQ744Eb");
            request.AddHeader("Authorization", "SSWS " + apiKey);
            request.AddHeader("Cookie", "JSESSIONID=A3BE173E706E0FF7AE787C92ABD698B3");
            request.AddParameter("text/plain", "", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            var parsedObject = JObject.Parse(response.Content);
            var profileJson = (parsedObject["profile"] ?? "").ToString();
            userProfile = JsonConvert.DeserializeObject<UserDto>(profileJson);
            return Ok(userProfile);
        }

        [HttpPost("{uid}", Name = "GetUserProfile")]
        public IActionResult UpdateUserProfile(string uid, UserDto userProfile)
        {
            var apiKey = _wineData.lk_Okta_API_Key.Select(x => x.API_Key).FirstOrDefault();
            if (apiKey == null) return NotFound();
            var client = new RestClient($"https://dev-364313.okta.com/api/v1/apps/0oa138hfqseIm0EpE4x7/users/" + uid);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "SSWS " + apiKey);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", "JSESSIONID=A3BE173E706E0FF7AE787C92ABD698B3");
            var requestBody = "{\"profile\": " + JsonConvert.SerializeObject(userProfile) + "}";
            request.AddParameter("application/json", requestBody, ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            var parsedObject = JObject.Parse(response.Content);
            var profileJson = parsedObject["profile"].ToString();
            userProfile = JsonConvert.DeserializeObject<UserDto>(profileJson);
            return Ok(userProfile);
        }
    }
}
