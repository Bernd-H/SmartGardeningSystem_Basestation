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
using GardeningSystem.Common.Specifications.Communication;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {

    /// <summary>
    /// API controller to login (get a json web token) or to change the login information.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase {

        private IConfiguration Configuration;

        private ISettingsManager SettingsManager;

        private IPasswordHasher PasswordHasher;

        private IAesEncrypterDecrypter AesDecrypter;

        private IWifiConfigurator WifiConfigurator;

        private IAPIManager APIManager;

        private IAesEncrypterDecrypter AesEncrypterDecrypter;

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
            AesEncrypterDecrypter = aesEncrypterDecrypter;
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login([FromBody] UserDto login) {
            Logger.Info($"[Login]User trying to log in.");
            IActionResult response = Unauthorized();

            if (!string.IsNullOrEmpty(login.Password) && !string.IsNullOrEmpty(login.Username)) {
                if (AuthenticateUser(login)) {
                    if (ValidateAesKey(login.KeyValidationBytes)) {
                        response = GenerateJSONWebToken();
                    }
                    else {
                        response = Conflict();
                    }
                }
            }

            return response;
        }

        // PUT api/<LoginController>
        [Authorize]
        [HttpPut]
        public IActionResult UpdateUser([FromBody] UpdateUserDto updateUserDto) {
            if (AuthenticateUser(new UserDto { Username = updateUserDto.Username, Password = updateUserDto.Password })) {
                Logger.Info($"[UpdateUser]Updating the login information.");

                // update login information
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.LoginSecrets = new LoginSecrets {
                        UserName = updateUserDto.NewUsername,
                        HashedPassword = PasswordHasher.HashPassword(Encoding.UTF8.GetBytes(updateUserDto.NewPassword))
                    };
                    return currentSettings;
                });

                return Ok();
            }

            return BadRequest();
        }

        private IActionResult GenerateJSONWebToken() {
            //Logger.Info($"[GenerateJSONWebToken]Generating json web token for user {userInfo.Id}.");
            Logger.Info($"[GenerateJSONWebToken]Generating a json web token.");

            try {
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration[ConfigurationVars.ISSUER_SIGNINGKEY]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new[] {
                    //new Claim(JwtClaimTypes.UserID, userInfo.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var token = new JwtSecurityToken(Configuration[ConfigurationVars.ISSUER],
                        Configuration[ConfigurationVars.ISSUER],
                        claims,
                        expires: DateTime.Now.AddMonths(1),
                        signingCredentials: credentials);

                string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
                return Ok(new { token = tokenString });
            } catch (Exception ex) {
                //Logger.Fatal(ex, $"[GenerateJSONWebToken]Error by creating Jwt for user {userInfo.Id}.");
                Logger.Fatal(ex, $"[GenerateJSONWebToken]An error occured while creating a Jwt.");
                return base.Problem("Error by creating a json web token.");
            }
        }

        private bool AuthenticateUser(UserDto userDto) {
            if (SettingsManager.GetApplicationSettings().LoginSecrets == null) {
                // set default login information
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    currentSettings.LoginSecrets = new LoginSecrets {
                        UserName = Configuration[ConfigurationVars.DEFAULTLOGIN_USERNAME],
                        HashedPassword = PasswordHasher.HashPassword(Encoding.UTF8.GetBytes(Configuration[ConfigurationVars.DEFAULTLOGIN_PASSWORD]))
                    };
                    return currentSettings;
                });
            }

            (bool verified, bool storedHashNeedsUpdate) = PasswordHasher.VerifyHashedPassword(
                SettingsManager.GetApplicationSettings().LoginSecrets.HashedPassword, Encoding.UTF8.GetBytes(userDto.Password));

            if (verified && storedHashNeedsUpdate) {
                // rehash password and store the new hash
                Logger.Info($"[AuthenticateUser]Updating stored password hash.");
                SettingsManager.UpdateCurrentSettings((currentSettings) => {
                    var hashedP = PasswordHasher.HashPassword(Encoding.UTF8.GetBytes(userDto.Password));
                    currentSettings.LoginSecrets.HashedPassword = hashedP;
                    return currentSettings;
                });
            }

            return verified;
        }

        private bool ValidateAesKey(byte[] keyValidationBytes) {
            var decryptedBytes = AesEncrypterDecrypter.DecryptToByteArray(keyValidationBytes);

            if (decryptedBytes.Length >= 2) {
                // extract the message from the decrypted bytes
                // (first two bytes are the code and the rest is a salt)
                var receivedCode = new byte[2];
                Array.Copy(decryptedBytes, 0, receivedCode, 0, receivedCode.Length);

                if (receivedCode.SequenceEqual(CommunicationCodes.KeyValidationMessage)) {
                    return true;
                }
            }

            return false;
        }

        #region old user authentication (user login information was stored on the external server)

        /// <summary>
        /// Authenticates a user by asking the external server for it's stored hash.
        /// Obsolete now. The username and password get's stored locally on this basestation now.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        //[Obsolete]
        //private bool AuthenticateUser_old(UserDto login) {
        //    bool result = false;

        //    // user authentication information is encrypted by a shared secret. Thats because the client can't know
        //    // in offline scenarios if the server is the real one or is behind a man in the middle.
        //    var userEmail = AesDecrypter.DecryptToByteArray(login.AesEncryptedEmail);
        //    if (userEmail.Length > 0) {
        //        Logger.Trace($"[AuthenticateUser]Checking if user with id={login.Id} exists.");

        //        // request userinformation from external server which holds all identities
        //        var user = APIManager.GetUserInfo(userEmail).Result;
        //        if (user != null) {
        //            login.Id = user.Id;

        //            //Validate the User Credentials
        //            var plainTextPassword = AesDecrypter.DecryptToByteArray(login.AesEncryptedPassword);
        //            if (plainTextPassword.Length > 0) {
        //                (bool valid, bool needsUpgrade) = PasswordHasher.VerifyHashedPassword(user.Id, user.HashedPassword, plainTextPassword);
        //                if (valid) {
        //                    result = true;

        //                    // check if upgrade needed
        //                    if (needsUpgrade) {
        //                        UpdateOutdatedHash(login.Id, userEmail, plainTextPassword);
        //                    }

        //                    Logger.Info($"[AuthenticateUser]User with id {user.Id} logged in.");
        //                }
        //                else {
        //                    Logger.Info($"[AuthenticateUser]User with id {user.Id} entered wrong password.");
        //                }
        //            }

        //            CryptoUtils.ObfuscateByteArray(plainTextPassword);
        //        }
        //        else {
        //            Logger.Info($"[AuthenticateUser]User with id={login.Id} not found.");
        //        }
        //    }

        //    return result;
        //}

        //[Obsolete]
        //private void UpdateOutdatedHash(Guid userId, byte[] email, byte[] plaintextPassword) {
        //    Logger.Info($"[UpdateOutdatedHash]Updating hash from user with id {userId}.");

        //    try {
        //        // update hash
        //        string newHash = PasswordHasher.HashPassword(plaintextPassword);

        //        var updated = APIManager.UpdateHash(new ChangeUserInfoDto {
        //            Id = userId,
        //            Email = email,
        //            PlainTextPassword = plaintextPassword,
        //            NewPasswordHash = newHash
        //        });
        //    } catch(Exception ex) {
        //        Logger.Error(ex, $"[UpdateOutdatedHash]Could not update hash from user {userId}.");
        //    }
        //}

        #endregion
    }
}
