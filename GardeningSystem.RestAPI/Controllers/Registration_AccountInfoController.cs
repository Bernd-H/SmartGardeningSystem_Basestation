using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.RestAPI.Filters;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/registration/account")]
    [ApiController]
    [CustomAuthenticationFilter]
    public class Registration_AccountInfoController : ControllerBase {

        private ILogger Logger;

        private ISettingsManager SettingsManager;

        public Registration_AccountInfoController(ILogger logger, ISettingsManager settingsManager) {
            Logger = logger;
            SettingsManager = settingsManager;
        }

        // GET: api/registration/account
        [HttpGet]
        public ActionResult<string> Get() {
            return "Test";
            //return Problem();
        }

        // POST api/registration/account
        [HttpPost]
        public IActionResult Post([FromBody] string value) {
            return Problem();
        }

        // DELETE api/registration/account/{email}
        [HttpDelete("{id}")]
        public IActionResult Delete(string id) {
            return Problem();
        }
    }
}
