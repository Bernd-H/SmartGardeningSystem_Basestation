using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase {

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        private IPasswordHasher PasswordHasher;

        private ILogger Logger;

        public LoginController(ILoggerService logger, IConfiguration config, ISettingsManager settingsManager, IPasswordHasher passwordHasher) {
            Logger = logger.GetLogger<LoginController>();
            Configuration = config;
            SettingsManager = settingsManager;
            PasswordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] UserDto login) {
            Logger.Info($"User with email {login.Email} trying to log in.");
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);

            if (user != null) {
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }

        private string GenerateJSONWebToken(UserDto userInfo) {
            Logger.Info($"Generating json web token for user {userInfo.Email}.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration[ConfigurationVars.ISSUER_SIGNINGKEY]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(Configuration[ConfigurationVars.ISSUER],
                Configuration[ConfigurationVars.ISSUER],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserDto AuthenticateUser(UserDto login) {
            Logger.Trace("In authenticate user methode.");
            UserDto result = null;

            // check if user is registered
            var user = SettingsManager.GetApplicationSettings().RegisteredUsers.ToList().Find(u => u.Email == login.Email);
            if (user != null) {
                //Validate the User Credentials    
                (bool valid, bool needsUpgrade) = PasswordHasher.VerifyHashedPassword(user.HashedPassword, login.PlainTextPassword);
                if (valid) {
                    result = login;

                    // check if upgrade needed
                    if (needsUpgrade) {
                        Logger.Info($"Updating hash from user {login.Email}.");

                        // update hash
                        string newHash = PasswordHasher.HashPassword(login.PlainTextPassword);

                        SettingsManager.UpdateCurrentSettings((currentSettings) => {
                            var registeredUsers = currentSettings.RegisteredUsers.ToList();
                            registeredUsers[registeredUsers.IndexOf(user)] = new User() {
                                Email = login.Email,
                                HashedPassword = newHash
                            };

                            // set new registerd users list
                            currentSettings.RegisteredUsers = registeredUsers;
                            return currentSettings;
                        });
                    }

                    Logger.Info("User password got validated.");
                } else {
                    Logger.Info("User password got rejected.");
                }
            }
            else {
                Logger.Info("User with email not found.");
            }

            return result;
        }
    }
}
