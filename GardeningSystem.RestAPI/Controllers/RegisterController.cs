using System;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Cryptography;
using GardeningSystem.Common.Specifications.Managers;
using GardeningSystem.Common.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace GardeningSystem.RestAPI.Controllers {

    /// <summary>
    /// Obsolete API controller. Was used to register a new user.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [Obsolete]
    public class RegisterController : ControllerBase {

        private IAesEncrypterDecrypter AesDecrypter;

        private IAPIManager APIManager;

        private IPasswordHasher PasswordHasher;

        private ILogger Logger;

        public RegisterController(ILoggerService loggerService, IAPIManager _APIManager, IAesEncrypterDecrypter aesEncrypterDecrypter, IPasswordHasher passwordHasher) {
            Logger = loggerService.GetLogger<RegisterController>();
            APIManager = _APIManager;
            AesDecrypter = aesEncrypterDecrypter;
            PasswordHasher = passwordHasher;
        }

        // POST api/<RegisterController>
        //[HttpPost]
        //public IActionResult RegisterUser([FromBody] UserDto userToAdd) {
        //    byte[] email = null, plainTextPwd = null;
        //    IActionResult result = Problem();

        //    try {
        //        email = AesDecrypter.DecryptToByteArray(userToAdd.AesEncryptedEmail);
        //        plainTextPwd = AesDecrypter.DecryptToByteArray(userToAdd.AesEncryptedPassword);

        //        // hash password
        //        var hashedPassword = PasswordHasher.HashPassword(plainTextPwd);

        //        result = APIManager.RegisterUser(new Common.Models.Entities.User {
        //            Id = userToAdd.Id,
        //            Email = email,
        //            HashedPassword = hashedPassword
        //        }).Result ? Ok() : Problem();
        //    }catch(Exception ex) {
        //        Logger.Error(ex, $"[RegisterUser]An error occured.");
        //    }
        //    finally {
        //        // obfuscate confidential data
        //        if (plainTextPwd != null) {
        //            CryptoUtils.ObfuscateByteArray(plainTextPwd);
        //        }
        //        if (email != null) {
        //            CryptoUtils.ObfuscateByteArray(email);
        //        }
        //    }

        //    return result;
        //}
    }
}
