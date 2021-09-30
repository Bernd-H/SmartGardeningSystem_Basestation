using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class ModulesController : ControllerBase {

        private IModulesRepository ModulesRepository;

        public ModulesController(IModulesRepository modulesRepository) {
            ModulesRepository = modulesRepository;
        }

        // GET: api/Modules
        [HttpGet]
        public IEnumerable<ModuleInfo> Get() {

            return ModulesRepository.GetAllRegisteredModules();
        }

        // GET api/Modules/{id}
        [HttpGet("{id}")]
        public ModuleInfo Get(int id) {
            return null;
            //return "value";
        }

        // POST api/Modules
        [HttpPost]
        public void Post([FromBody] string value) {
        }

        // PUT api/Modules/{id}
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value) {
        }

        // DELETE api/Modules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) {
            return NotFound();
        }
    }
}
