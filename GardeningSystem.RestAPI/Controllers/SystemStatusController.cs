using System;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Utilities;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class SystemStatusController : ControllerBase {

        private ILogger Logger;

        public SystemStatusController(ILoggerService loggerService) {
            Logger = loggerService.GetLogger<SystemStatusController>();
        }

        // GET: api/<SystemStatusController>
        [HttpGet]
        public SystemStatusDto Get() {
            Logger.Info($"[Get]User {ControllerHelperClass.GetUserId(HttpContext)} requested system status.");
            try {
                Logger.Warn($"[Get]Mocking some system status properties.");
                return new SystemStatusDto {
                    SystemUpMinutes = TimeUtils.GetUpTimeInMinutes(),
                    Temperature = 22,
                    WateringStatus = WateringStatus.Ready
                };
            }
            catch (Exception ex) {
                Logger.Error(ex, "[Get]Could not load system status properties.");

                return null;
            }
        }
    }
}
