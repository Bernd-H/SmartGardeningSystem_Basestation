using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using Newtonsoft.Json;
using NLog;

namespace GardeningSystem.BusinessLogic.Managers {

    /// <inheritdoc/>
    public class APIManager : IAPIManager {

        private readonly HttpClient client;


        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public APIManager(ILoggerService loggerService, ISettingsManager settingsManager) {
            Logger = loggerService.GetLogger<APIManager>();
            SettingsManager = settingsManager;

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = CertificateValidationCallback;
            client = new HttpClient(httpClientHandler);

            // add json web token to header if a token exists
            TryAddWebTokenToHeader();
        }

        /// <inheritdoc/>
        public async Task GetToken() {
            Logger.Info($"[GetToken]Trying to get a token from a server in the local network.");
            string url = "";

            try {
                // build url
                var config = ConfigurationContainer.Configuration;
                url = string.Format(config[ConfigurationVars.EXTERNALSERVER_GETTOKEN_URL], config[ConfigurationVars.EXTERNALSERVER_LOCALIP], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);

                // setup the body of the request
                string json = $"\"{SettingsManager.GetApplicationSettings().Id.ToString()}\"";
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, data);
                
                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) {
                    string result = await response.Content.ReadAsStringAsync();
                    if (result.Contains("token")) {
                        Logger.Info($"[GetToken]Received api token from server.");
                        var jwt = JsonConvert.DeserializeObject<Jwt>(result);

                        // store json web token in settings
                        SettingsManager.UpdateCurrentSettings(currentSettings => {
                            currentSettings.APIToken = jwt.Token;
                            return currentSettings;
                        });

                        // add web token to request header
                        TryAddWebTokenToHeader();
                    }
                    else {
                        Logger.Fatal($"[GetToken]Something went wrong. StatusCode={response.StatusCode}.");
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[GetToken]Could not get token from rest api. (url={url})");
            }
        }

        /// <inheritdoc/>
        public async Task<WeatherForecast> GetWeatherForecast(string location) {
            Logger.Trace($"[GetWeather]Trying to get a weather forecast.");
            string url = "";

            try {
                // build url
                var config = ConfigurationContainer.Configuration;
                url = string.Format(config[ConfigurationVars.EXTERNALSERVER_WEATHER_URL], config[ConfigurationVars.EXTERNALSERVER_DOMAIN], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);
                url += location;

                var response = await client.GetAsync(url);

                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) {
                    string result = await response.Content.ReadAsStringAsync();
                    var weatherForecast = JsonConvert.DeserializeObject<WeatherForecast>(result);

                    return weatherForecast;
                } 
                else {
                    Logger.Error($"[GetWeather]Unauthorized.");
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[GetWeather]Something went wrong. (url={url})");
            }

            return null;
        }

        #region UserController-Methods

        //public async Task<User> GetUserInfo(byte[] email) {
        //    Logger.Info($"[GetUserInfo]Requesting user information from external server.");

        //    if (!client.DefaultRequestHeaders.Contains("Authorization")) {
        //        Logger.Fatal($"[GetUserInfo]Unable perfom this api request with no json web token.");
        //        return null;
        //    }

        //    string url = "";

        //    try {
        //        // build url
        //        var config = ConfigurationContainer.Configuration;
        //        url = string.Format(config[ConfigurationVars.EXTERNALSERVER_USER_URL], config[ConfigurationVars.EXTERNALSERVER_DOMAIN], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);
        //        url += $"{Convert.ToBase64String(email)}";

        //        var response = await client.GetAsync(url);
        //        string result = await response.Content.ReadAsStringAsync();

        //        return JsonConvert.DeserializeObject<User>(result);
        //    }
        //    catch (Exception ex) {
        //        Logger.Error(ex, $"[GetUserInfo]Could not get user details from api. (url={url})");
        //    }

        //    return null;
        //}

        //public async Task<bool> RegisterUser(User user) {
        //    Logger.Info($"[RegisterUser]Adding a new user to the external server.");

        //    if (!client.DefaultRequestHeaders.Contains("Authorization")) {
        //        Logger.Fatal($"[RegisterUser]Unable perfom this api request with no json web token.");
        //        return false;
        //    }

        //    string url = "";

        //    try {
        //        // build url
        //        var config = ConfigurationContainer.Configuration;
        //        url = string.Format(config[ConfigurationVars.EXTERNALSERVER_USER_URL], config[ConfigurationVars.EXTERNALSERVER_DOMAIN], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);

        //        // prepare data to send
        //        string json = JsonConvert.SerializeObject(user);

        //        // setup the body of the request
        //        StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await client.PostAsync(url, data);

        //        if (response.StatusCode == System.Net.HttpStatusCode.OK) {
        //            return true;
        //        }
        //    }
        //    catch (Exception ex) {
        //        Logger.Error(ex, $"[RegisterUser]Could not get user details from api. (url={url})");
        //    }

        //    return false;
        //}

        //public async Task<bool> UpdateHash(ChangeUserInfoDto updatedUserInfo) {
        //    Logger.Info($"[UpdateHash]Updating hashed password for user with id {updatedUserInfo.Id} in external database.");

        //    if (!client.DefaultRequestHeaders.Contains("Authorization")) {
        //        Logger.Fatal($"[UpdateHash]Unable perfom this api request with no json web token.");
        //        return false;
        //    }

        //    string url = "";

        //    try {
        //        // build url
        //        var config = ConfigurationContainer.Configuration;
        //        url = string.Format(config[ConfigurationVars.EXTERNALSERVER_USER_URL], config[ConfigurationVars.EXTERNALSERVER_DOMAIN], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);

        //        // prepare data to send
        //        string json = JsonConvert.SerializeObject(updatedUserInfo);

        //        // setup the body of the request
        //        StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

        //        var response = await client.PutAsync(url, data);

        //        if (response.StatusCode == System.Net.HttpStatusCode.OK) {
        //            return true;
        //        }
        //        else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized) {
        //            return false;
        //        }
        //        else {
        //            Logger.Error($"[UpdateHash]API returned code: {response.StatusCode.ToString()}.");
        //        }
        //    }
        //    catch (Exception ex) {
        //        Logger.Error(ex, $"[UpdateHash]Could not update a hash from rest api. (url={url})");
        //    }

        //    return false;
        //}

        #endregion

        /// <inheritdoc/>
        public async Task<bool> UpdateIPStatus(IPStatusDto dto) {
            Logger.Info($"[UpdateIPStatus]Transmitting public ip to external server.");

            if (!client.DefaultRequestHeaders.Contains("Authorization")) {
                Logger.Fatal($"[UpdateIPStatus]Unable perfom this api request with no json web token.");
                return false;
            }

            string url = "";

            try {
                // build url
                var config = ConfigurationContainer.Configuration;
                url = string.Format(config[ConfigurationVars.EXTERNALSERVER_IPLOOKUP_URL], config[ConfigurationVars.EXTERNALSERVER_DOMAIN], config[ConfigurationVars.EXTERNALSERVER_APIPORT]);

                // prepare data to send
                string json = JsonConvert.SerializeObject(dto);

                // setup the body of the request
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, data);

                if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, $"[UpdateIPStatus]Could not update public ip address. (url={url})");
            }

            return false;
        }

        private bool CertificateValidationCallback(HttpRequestMessage httpReqMessage, X509Certificate2 cert, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
            if (ConfigurationContainer.Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT] == "true") {
                Logger.Warn($"[CertificateValidationCallback]Not validating certificate.");
                return true;
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if (httpReqMessage.RequestUri.Host.Contains(ConfigurationContainer.Configuration[ConfigurationVars.EXTERNALSERVER_LOCALIP])) {
                // certificate is invalid, if the external server api gets accessed directly within the network threw a local ip
                // instead of it's domainname
                return true;
            }

            Logger.Warn("[ValidateServerCertificate]Certificate error: {0}", sslPolicyErrors);

            // do not allow this client to communicate with this unauthenticated server.
            return false;
        }

        private bool TryAddWebTokenToHeader() {
            var settings = SettingsManager.GetApplicationSettings();
            if (!string.IsNullOrEmpty(settings.APIToken)) {
                // remove existing authorization header
                if (client.DefaultRequestHeaders.Contains("Authorization")) {
                    client.DefaultRequestHeaders.Remove("Authorization");
                }

                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.APIToken}");
                return true;
            }

            return false;
        }
    }
}
