using System;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Managers {

    /// <summary>
    /// Class that manages Api requests to the external server.
    /// </summary>
    public interface IAPIManager {

        /// <summary>
        /// Trys to register at the external server to get a json web token, needed as authentication for all other api requests.
        /// </summary>
        /// <returns>A task that represents a asynchronous operation.</returns>
        /// <remarks>Sends this request to a specific private ip address. So this json web token (Jwt) can only be retrieved when the basestation
        /// gets started in the same network, as the external server. Made to exchange the Jwt, which has no expiration date, while the production
        /// of the basestation. The server will block all GetToken requests that are comming form IPs that are not private.</remarks>
        Task GetToken();

        //Task<User> GetUserInfo(byte[] email);
        //Task<bool> RegisterUser(User user);
        /// <exception cref="UnauthorizedAccessException">Gets thrown when token or the plain text password is not valid.</exception>
        //Task<bool> UpdateHash(ChangeUserInfoDto updatedUserInfo);

        /// <summary>
        /// Updates the public ip address of this system, which gets stored on the external server.
        /// This IP is used by the external server to redirect the mobile app to this gardening system basestation.
        /// </summary>
        /// <param name="dto">Public ip address and basestation id.</param>
        /// <returns>A task that represents a asynchronous operation.</returns>
        [Obsolete]
        Task<bool> UpdateIPStatus(IPStatusDto dto);

        /// <summary>
        /// Requests the amount of rain in mm for the next day.
        /// </summary>
        /// <param name="location">Location of a near city.</param>
        /// <returns>A task that represents a asynchronous operation. The value of the TResult
        /// parameter contains a WeatherForecast object.</returns>
        /// <seealso cref="WeatherForecast"/>
        Task<WeatherForecast> GetWeatherForecast(string location);
    }
}
