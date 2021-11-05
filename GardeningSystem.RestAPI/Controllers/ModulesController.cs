using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModulesController : ControllerBase {

        private IModuleManager ModuleManager;

        private ILogger Logger;

        public ModulesController(ILoggerService logger, IModuleManager moduleManager) {
            Logger = logger.GetLogger<ModulesController>();
            ModuleManager = moduleManager;
        }

        // GET: api/Modules
        [HttpGet]
        public IEnumerable<ModuleInfoDto> Get() {
            Logger.Info($"[Get]User {ControllerHelperClass.GetUserId(HttpContext)} requested all registered modules.");
            try {
                return ModuleManager.GetAllModules().Result;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[Get]Could not load all registered modules.");

                return null;
            }
        }

        // GET api/Modules/{id}
        [HttpGet("{id}")]
        public ActionResult<ModuleInfoDto> Get(Guid id) {
            var userId = ControllerHelperClass.GetUserId(HttpContext);
            Logger.Info($"[Get]User {userId} requested registered module with id={id}.");

            ModuleInfoDto module;
            try {
                module = ModuleManager.GetModuleById(id).Result;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[Get]Could not load requested module information.");

                return Problem(ex.Message);
            }

            if (module == null) {
                return NotFound();
            }
            return module;
        }

        // Used to add a new module
        // POST api/Modules
        [HttpPost]
        public IActionResult Post([FromBody] ModuleInfoDto value) {
            var userId = ControllerHelperClass.GetUserId(HttpContext);
            Logger.Info($"[Post]User {userId} send new module info to add.");

            try {
                ModuleManager.AddModule(value);
            }
            catch (Exception ex) {
                Logger.Error(ex, "[Post]Could not add new module info.");

                return Problem(ex.Message);
            }

            return Ok();
        }

        // Used to update a already existing module
        // PUT api/Modules/{id}
        [HttpPut("{id}")]
        public IActionResult Put(Guid id, [FromBody] ModuleInfoDto value) {
            var userId = ControllerHelperClass.GetUserId(HttpContext);
            Logger.Info($"[Put]User {userId} send module info to update (module id={value.Id}).");

            // check request
            if (id != value.Id) {
                Logger.Trace($"[Put]Returned bad request.");
                return BadRequest();
            }

            // update module
            try {
                if (ModuleManager.UpdateModule(value).Result) {
                    return Ok();
                }
                else {
                    Logger.Error($"[Put]Modulel with id={id} not found.");
                    return NotFound();
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[Put]Could not update module with id={id}.");
                return Problem(ex.Message);
            }
        }

        // DELETE api/Modules/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id) {
            var userId = ControllerHelperClass.GetUserId(HttpContext);
            Logger.Info($"[Delete]User {userId} requested to delete module with id={id}.");

            bool deleted = false;
            try {
                deleted = ModuleManager.RemoveModule(id).Result;
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[Delete]Could not delete module with id={id}.");

                return Problem(ex.Message);
            }

            if (deleted) {
                return Ok();
            }
            else {
                return NotFound();
            }
        }
    }
}
