using System;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Managers {
    public interface IAPIManager {

        Task GetToken();

        Task<User> GetUserInfo(byte[] email);

        Task<bool> RegisterUser(User user);

        /// <exception cref="UnauthorizedAccessException">Gets thrown when token or the plain text password is not valid.</exception>
        Task<bool> UpdateHash(ChangeUserInfoDto updatedUserInfo);
    }
}
