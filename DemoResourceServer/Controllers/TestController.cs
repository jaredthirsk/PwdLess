using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DemoResourceServer.Controllers
{
    [Route("[controller]/[action]")]
    public class TestController : Controller
    {
        [HttpGet]
        public IActionResult Open(string text)
        {
            return Ok(text);
        }

        [HttpGet]
        [Authorize]
        public IActionResult Closed(string text)
        {
            return Ok(text);
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IActionResult Admins(string text)
        {
            return Ok(text);
        }
    }
}
