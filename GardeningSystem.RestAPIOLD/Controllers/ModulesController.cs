using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModulesController : ControllerBase {

        private IModulesRepository ModulesRepository;

        private ILogger Logger;

        public ModulesController(ILogger logger, IModulesRepository modulesRepository) {
            Logger = logger;
            ModulesRepository = modulesRepository;
        }

        // GET: api/Modules
        [HttpGet]
        public IEnumerable<ModuleInfo> Get() {
            try {
                return ModulesRepository.GetAllRegisteredModules();
            } catch(Exception ex) {
                Logger.Error(ex, "[RestAPI.ModulesController.Get]");

                return null;
            }
        }

        // GET api/Modules/{id}
        [HttpGet("{id}")]
        public ActionResult<ModuleInfo> Get(Guid id) {
            ModuleInfo module;
            try {
                module = ModulesRepository.GetModuleById(id);
            } catch(Exception ex) {
                Logger.Error(ex, "[RestAPI.ModulesController.GetById]");

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
        public IActionResult Post([FromBody] ModuleInfo value) {
            try {
                ModulesRepository.AddModule(value);
            } catch(Exception ex) {
                Logger.Error(ex, "[RestAPI.ModulesController.Post]");

                return Problem(ex.Message);
            }

            return Ok();
        }

        // Used to update a already existing module
        // PUT api/Modules/{id}
        [HttpPut("{id}")]
        public IActionResult Put(Guid id, [FromBody] ModuleInfo value) {
            if (id != value.Id)
                return BadRequest();

            if (ModulesRepository.UpdateModule(value)) {
                return Ok();
            }

            return Problem();
        }

        // DELETE api/Modules/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id) {
            bool deleted = false;
            try {
                deleted = ModulesRepository.RemoveModule(id);
            } catch(Exception ex) {
                Logger.Error(ex, "[RestAPI.ModulesController.Delete]");

                return Problem(ex.Message);
            }

            if (deleted) {
                return Ok();
            }
            
            return NotFound();
        }
    }
}
