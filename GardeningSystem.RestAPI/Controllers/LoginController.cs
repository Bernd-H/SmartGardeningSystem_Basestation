using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.BusinessLogic.Cryptography;
using GardeningSystem.Common;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
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

        private IWifiConfigurator WifiConfigurator;

        private IAPIManager APIManager;

        private ILogger Logger;

        public LoginController(ILoggerService logger, IConfiguration config, ISettingsManager settingsManager, IPasswordHasher passwordHasher,
            IAesEncrypterDecrypter aesEncrypterDecrypter, IWifiConfigurator wifiConfigurator, IAPIManager _APIManager) {
            Logger = logger.GetLogger<LoginController>();
            Configuration = config;
            SettingsManager = settingsManager;
            PasswordHasher = passwordHasher;
            AesDecrypter = aesEncrypterDecrypter;
            WifiConfigurator = wifiConfigurator;
            APIManager = _APIManager;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] UserDto login) {
            Logger.Info($"[Login]User trying to log in.");
            IActionResult response = Unauthorized();

            bool hasInternet = WifiConfigurator.HasInternet();

            // no login requierd if the basestation has no internet
            if (!hasInternet || AuthenticateUser(login)) {
                response = GenerateJSONWebToken(login);
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

        private bool AuthenticateUser(UserDto login) {
            bool result = false;

            // user authentication information is encrypted by a shared secret. Thats because the client can't know
            // in offline scenarios if the server is the real one or is behind a man in the middle.
            var userEmail = AesDecrypter.DecryptToByteArray(login.AesEncryptedEmail);
            if (userEmail.Length > 0) {
                Logger.Trace($"[AuthenticateUser]Checking if user with id={login.Id} exists.");

                // request userinformation from external server which holds all identities
                var user = APIManager.GetUserInfo(userEmail).Result;
                if (user != null) {
                    login.Id = user.Id;

                    //Validate the User Credentials
                    var plainTextPassword = AesDecrypter.DecryptToByteArray(login.AesEncryptedPassword);
                    if (plainTextPassword.Length > 0) {
                        (bool valid, bool needsUpgrade) = PasswordHasher.VerifyHashedPassword(user.Id, user.HashedPassword, plainTextPassword);
                        if (valid) {
                            result = true;

                            // check if upgrade needed
                            if (needsUpgrade) {
                                UpdateOutdatedHash(login.Id, userEmail, plainTextPassword);
                            }

                            Logger.Info($"[AuthenticateUser]User with id {user.Id} logged in.");
                        }
                        else {
                            Logger.Info($"[AuthenticateUser]User with id {user.Id} entered wrong password.");
                        }
                    }

                    CryptoUtils.ObfuscateByteArray(plainTextPassword);
                }
                else {
                    Logger.Info($"[AuthenticateUser]User with id={login.Id} not found.");
                }
            }

            return result;
        }

        private void UpdateOutdatedHash(Guid userId, byte[] email, byte[] plaintextPassword) {
            Logger.Info($"[UpdateOutdatedHash]Updating hash from user with id {userId}.");

            try {
                // update hash
                string newHash = PasswordHasher.HashPassword(plaintextPassword);

                var updated = APIManager.UpdateHash(new ChangeUserInfoDto {
                    Id = userId,
                    Email = email,
                    PlainTextPassword = plaintextPassword,
                    NewPasswordHash = newHash
                });
            } catch(Exception ex) {
                Logger.Error(ex, $"[UpdateOutdatedHash]Could not update hash from user {userId}.");
            }
        }
    }
}
