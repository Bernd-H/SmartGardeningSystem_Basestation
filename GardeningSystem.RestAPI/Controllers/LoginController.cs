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

        private IAesEncrypterDecrypter AesDecrypter;

        private ILogger Logger;

        public LoginController(ILoggerService logger, IConfiguration config, ISettingsManager settingsManager, IPasswordHasher passwordHasher,
            IAesEncrypterDecrypter aesEncrypterDecrypter) {
            Logger = logger.GetLogger<LoginController>();
            Configuration = config;
            SettingsManager = settingsManager;
            PasswordHasher = passwordHasher;
            AesDecrypter = aesEncrypterDecrypter;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] UserDto login) {
            Logger.Info($"[Login]User trying to log in.");
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
            UserDto result = null;

            // user authentication information is encrypted by a shared secret. Thats because the client can't know
            // in offline scenarios if the server is the real one or is behind a man in the middle.
            var userEmail = Encoding.UTF8.GetString(AesDecrypter.DecryptToByteArray(login.AesEncryptedEmail));
            if (!string.IsNullOrEmpty(userEmail)) {
                Logger.Trace($"[AuthenticateUser]Checking if user with email {userEmail.Substring(0, userEmail.IndexOf('.'))}.* exists.");

                // check if user is registered
                var user = SettingsManager.GetApplicationSettings().RegisteredUsers.ToList().Find(u => u.Email == userEmail);
                if (user != null) {
                    login.Id = user.Id;

                    //Validate the User Credentials
                    var plainTextPassword = Encoding.UTF8.GetString(AesDecrypter.DecryptToByteArray(login.AesEncryptedPassword)); // TODO: unsafe
                    if (!string.IsNullOrEmpty(plainTextPassword)) {
                        (bool valid, bool needsUpgrade) = PasswordHasher.VerifyHashedPassword(user.Id, user.HashedPassword, plainTextPassword);
                        if (valid) {
                            result = login;

                            // check if upgrade needed
                            if (needsUpgrade) {
                                UpdateOutdatedHash(login.Id, userEmail, plainTextPassword);
                            }

                            Logger.Info($"[AuthenticateUser]User with id {user.Id} logged in.");
                        }
                        else {
                            Logger.Info($"[AuthenticateUser]User with id {user.Id} entered wrong password.");
                        }
                    } // password null or empty - decryption error / password encrypted with wrong aes key
                    
                }
                else {
                    Logger.Info($"User with email {userEmail.Substring(0, userEmail.IndexOf('.'))}.* not found.");
                }
            }

            return result;
        }

        private void UpdateOutdatedHash(Guid userId, string email, string plaintextPassword) {
            Logger.Info($"[UpdateOutdatedHash]Updating hash from user with id {userId}.");

            try {
                // update hash
                string newHash = PasswordHasher.HashPassword(plaintextPassword);

                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    var registeredUsers = currentSettings.RegisteredUsers.ToList();
                    var registeredUser = registeredUsers.Find(u => u.Id == userId);
                    registeredUsers[registeredUsers.IndexOf(registeredUser)] = new User() {
                        Email = email,
                        HashedPassword = newHash
                    };

                    // set new registerd users list
                    currentSettings.RegisteredUsers = registeredUsers;
                    return currentSettings;
                });
            } catch(Exception ex) {
                Logger.Error(ex, $"[UpdateOutdatedHash]Could not update hash from user {userId}.");
            }
        }
    }
}
