using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using SharpCR.Registry.Controllers.ResponseModels;
using SharpCR.Registry.Models;

namespace SharpCR.Registry.Controllers
{
    public class TagController
    {
        [RegistryRoute("tags/list")]
        [HttpGet]
        public TagListResponse List(string repo, [FromQuery]int? n, [FromQuery]string last)
        {
            return new TagListResponse
            {
                name = repo,
                tags = new List<string>()
            };
        }
    }
}