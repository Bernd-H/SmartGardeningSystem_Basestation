﻿using System;
using System.Collections.Generic;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WlanController : ControllerBase {

        private ILogger Logger;

        private IWifiConfigurator WifiConfigurator;

        public WlanController(ILoggerService logger, IWifiConfigurator wifiConfigurator) {
            Logger = logger.GetLogger<WlanController>();
            WifiConfigurator = wifiConfigurator;
        }

        // GET: api/Wlan
        [HttpGet]
        public IEnumerable<WlanInfo> Get() {
            Logger.Info($"[Get]User {ControllerHelperClass.GetUserId(HttpContext)} requested all registered modules.");
            try {
                return WifiConfigurator.GetAllWlans();
            }
            catch (Exception ex) {
                Logger.Error(ex, "[Get]Could not load all registered modules.");

                return null;
            }
        }

        // GET api/Wlan/{isConnected}
        [HttpGet("{isConnected}")]
        public ActionResult<IsConnectedToWlanDto> Get(string isConnected) {
            var userId = ControllerHelperClass.GetUserId(HttpContext);
            Logger.Info($"[GetIsConnected]User {userId} requested information if basestation is connected to a wlan.");

            bool _isConnected = false;
            try {
                _isConnected = WifiConfigurator.IsConnectedToWlan();
            }
            catch (Exception ex) {
                Logger.Error(ex, "[GetIsConnected]Could not load requested module information.");

                return Problem(ex.Message);
            }

            return new IsConnectedToWlanDto {
                IsConnected = _isConnected
            };
        }
    }
}
