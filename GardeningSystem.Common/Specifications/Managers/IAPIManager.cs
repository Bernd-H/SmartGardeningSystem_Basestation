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

        /// <summary>
        /// Updates the public ip address of this system, which gets stored on the external server.
        /// This IP is used by the external server to redirect the mobile app to this gardening system basestation.
        /// </summary>
        /// <param name="dto">Public ip address and basestation id.</param>
        /// <returns></returns>
        Task<bool> UpdateIPStatus(IPStatusDto dto);
    }
}
