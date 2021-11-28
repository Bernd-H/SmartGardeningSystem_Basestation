using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Managers;
using NLog;

namespace GardeningSystem.Jobs {
    public class PublicIPJob : TimedHostedService {

        static readonly TimeSpan PERIOD = TimeSpan.FromMinutes(1);

        private string _lastSendIP = string.Empty;


        private IAPIManager APIManager;

        private ISettingsManager SettingsManager;

        private ILogger Logger;

        public PublicIPJob(ILoggerService logger, IAPIManager _APIManager, ISettingsManager settingsManager) 
            : base(logger, nameof(PublicIPJob), PERIOD, waitTillDoWorkHasFinished: true) {
            Logger = logger.GetLogger<PublicIPJob>();
            APIManager = _APIManager;
            SettingsManager = settingsManager;

            base.SetEventHandler(new EventHandler(Start));
            Logger.Info($"[PublicIPJob]Informing external server when the public ip address changed.");
        }

        private void Start(object s, EventArgs e) {
            var ip = GetIp();
            if (!string.IsNullOrEmpty(ip)) {
                if (ip != _lastSendIP) {
                    // send current ip to server
                    Logger.Info($"[Start]Transmitting ip to server.");

                    bool success = APIManager.UpdateIPStatus(new Common.Models.DTOs.IPStatusDto {
                        Id = SettingsManager.GetApplicationSettings().Id,
                        Ip = ip
                    }).Result;

                    if (success) {
                        _lastSendIP = ip;
                    }
                    else {
                        Logger.Warn($"[Start]IP transmit has failed.");
                    }
                }
            }
        }

        private string GetIp() {
            try {
                var request = HttpWebRequest.Create("https://www.cloudflare.com/cdn-cgi/trace");
                request.Method = "GET";

                var response = request.GetResponse();

                using (var reader = new StreamReader(response.GetResponseStream())) {
                    var answer = reader.ReadToEnd();
                    return ExtractIp(answer);
                }
            }
            catch (Exception) {
                return string.Empty;
            }
        }

        private string ExtractIp(string answer) {
            var regex = new Regex(@"ip=(?'ip'([0-9]{1,3}\.){3}[0-9]{1,3})", RegexOptions.Compiled);

            var match = regex.Match(answer);
            if (!match.Success) return null;

            return match.Groups["ip"].Value;
        }
    }
}
