using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
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
            Logger.Info($"[Login]User with email {login.Email.Substring(0, login.Email.IndexOf('.'))}.* trying to log in.");
            IActionResult response = Unauthorized();
            var user = AuthenticateUser(login);

            if (user != null) {
                response = GenerateJSONWebToken(user);
            }

            return response;
        }

        private IActionResult GenerateJSONWebToken(UserDto userInfo) {
            Logger.Info($"[GenerateJSONWebToken]Generating json web token for user {userInfo.Id}.");

            try {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration[ConfigurationVars.ISSUER_SIGNINGKEY]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[] {
                    //new Claim(JwtRegisteredClaimNames.NameId, userInfo.Id.ToString()),
                    new Claim(JwtClaimTypes.UserID, userInfo.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(Configuration[ConfigurationVars.ISSUER],
                        Configuration[ConfigurationVars.ISSUER],
                        claims,
                        expires: DateTime.Now.AddMinutes(120),
                        signingCredentials: credentials);

                string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(new { token = tokenString });
            } catch (Exception ex) {
                Logger.Fatal(ex, $"[GenerateJSONWebToken]Error by creating Jwt for user {userInfo.Id}.");
                return base.Problem("Error by creating a json web token.");
            }
        }

        private UserDto AuthenticateUser(UserDto login) {
            Logger.Trace($"[AuthenticateUser]Checking if user with email {login.Email.Substring(0, login.Email.IndexOf('.'))}.* exists.");
            UserDto result = null;

            // check if user is registered
            var user = SettingsManager.GetApplicationSettings().RegisteredUsers.ToList().Find(u => u.Email == login.Email);
            if (user != null) {
                login.Id = user.Id;

                //Validate the User Credentials    
                (bool valid, bool needsUpgrade) = PasswordHasher.VerifyHashedPassword(user.ToDto(), user.HashedPassword, login.PlainTextPassword);
                if (valid) {
                    result = login;

                    // check if upgrade needed
                    if (needsUpgrade) {
                        UpdateOutdatedHash(login);
                    }

                    Logger.Info($"[AuthenticateUser]User with id {user.Id} logged in.");
                } else {
                    Logger.Info($"[AuthenticateUser]User with id {user.Id} entered wrong password.");
                }
            }
            else {
                Logger.Info($"User with email {login.Email.Substring(0, login.Email.IndexOf('.'))}.* not found.");
            }

            return result;
        }

        private void UpdateOutdatedHash(UserDto userLogin) {
            Logger.Info($"[UpdateOutdatedHash]Updating hash from user with id {userLogin.Id}.");

            try {
                // update hash
                string newHash = PasswordHasher.HashPassword(userLogin.PlainTextPassword);

                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    var registeredUsers = currentSettings.RegisteredUsers.ToList();
                    var registeredUser = registeredUsers.Find(u => u.Id == userLogin.Id);
                    registeredUsers[registeredUsers.IndexOf(registeredUser)] = new User() {
                        Email = userLogin.Email,
                        HashedPassword = newHash
                    };

                    // set new registerd users list
                    currentSettings.RegisteredUsers = registeredUsers;
                    return currentSettings;
                });
            } catch(Exception ex) {
                Logger.Error(ex, $"[UpdateOutdatedHash]Could not update hash from user {userLogin.Id}.");
            }
        }
    }
}
