using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess {
    public class WifiConfigurator : IWifiConfigurator {

        private ILogger Logger;

        private IConfiguration Configuration;

        public WifiConfigurator(ILoggerService logger, IConfiguration configuration) {
            Logger = logger.GetLogger<WifiConfigurator>();
            Configuration = configuration;
        }

        public bool ConnectToWlan(string essid, byte[] secret) {
            Logger.Info($"[ConnectToWlan]Trying to connect to wlan wiht essid={essid}.");

            // connect to wlan
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = $"/nmcli d wifi connect {essid} password {secret}" };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();

            // check if connected
            return IsConnectedToWlan();
        }

        public IEnumerable<string> GetAllWlans() {
            Logger.Info($"[GetAllWlans]Searching for reachable wifis.");

            // get all wlans
            string command = $"sudo iwlist {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} scan | grep ESSID";
            ProcessStartInfo startInfo = new ProcessStartInfo() {
                FileName = "/bin/bash",
                Arguments = "/" + command,
                RedirectStandardOutput = true
            };
            Process proc = new Process() { StartInfo = startInfo };
            proc.Start();
            var streamReader = proc.StandardOutput;

            string allWlans = streamReader.ReadToEnd();

            // parse essids
            var finalIds = new List<string>();
            var essids = allWlans.Split("ESSID:\"").ToList();
            essids.RemoveAt(0);
            foreach (var id in essids) {
                finalIds.Add(id.Substring(0, id.IndexOf("\"")));
            }

            return finalIds;
        }

        public bool HasInternet() {
            Logger.Info($"[HasInternet]Checking internet connection.");
            throw new NotImplementedException();
        }

        public bool IsConnectedToWlan() {
            Logger.Info($"[IsConnectedToWlan]Checking wifi connection.");
            throw new NotImplementedException();
        }
    }
}
