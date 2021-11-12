using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess {
    public class WifiConfigurator : IWifiConfigurator {

        static string changeWlanScriptName = "changeWlanScript";


        private ILogger Logger;

        private IConfiguration Configuration;

        public WifiConfigurator(ILoggerService logger, IConfiguration configuration) {
            Logger = logger.GetLogger<WifiConfigurator>();
            Configuration = configuration;
        }

        public bool ConnectToWlan(string essid, string secret) {
            Logger.Info($"[ConnectToWlan]Trying to connect to wlan wiht essid={essid}.");
            setScriptExecutionRights();

            // connect to wlan
            executeCommand($"sudo ./{changeWlanScriptName} \"{essid}\" \"{secret}\"");

            // check if connected
            bool success = false;
            int attempts = 5;
            do {
                if (attempts != 5) {
                    Thread.Sleep(2000);
                }
                success = IsConnectedToWlan();
                attempts--;
            } while (!success && attempts >= 0);

            if (!success) {
                // TODO: establish connection 
            }

            return success;
        }

        public IEnumerable<string> GetAllWlans() {
            Logger.Info($"[GetAllWlans]Searching for reachable wifis.");
            var finalIds = new List<string>();

            try { 
                // get all wlans
                string command = $"sudo iwlist {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} scan | grep ESSID";
                ProcessStartInfo startInfo = new ProcessStartInfo() {
                    FileName = "/bin/bash",
                    Arguments = "/" + command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                Process proc = new Process() { StartInfo = startInfo };
                proc.Start();
                var streamReader = proc.StandardOutput;

                string allWlans = streamReader.ReadToEnd();

                // parse essids
                var essids = allWlans.Split("ESSID:\"").ToList();
                essids.RemoveAt(0);
                foreach (var id in essids) {
                    finalIds.Add(id.Substring(0, id.IndexOf("\"")));
                }

            }
            catch (Exception ex) {
                Logger.Error(ex, "[GetAllWlans]Exception while getting/pareing ssids.");
            }

            return finalIds;
        }

        public bool HasInternet() {
            Logger.Info($"[HasInternet]Checking internet connection.");
            return pingHost("google.com");
        }

        public bool IsConnectedToWlan() {
            Logger.Info($"[IsConnectedToWlan]Checking wifi connection.");

            throw new NotImplementedException();
        }

        private static bool pingHost(string nameOrAddress) {
            bool pingable = false;
            Ping pinger = null;

            try {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException) {
                // Discard PingExceptions and return false;
            }
            finally {
                if (pinger != null) {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        private static void setScriptExecutionRights() {
            try {
                executeCommand($"chmod +x {changeWlanScriptName}");
            }
            catch (Exception) { }
        }

        private static void executeCommand(string command) {
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = $"/{command}", UseShellExecute = false };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();
        }
    }
}
