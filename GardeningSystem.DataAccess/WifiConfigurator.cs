using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Specifications;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess {
    public class WifiConfigurator : IWifiConfigurator {

        static string changeWlanScriptName = "changeWlanScript";

        static string disconnectFromWlanScriptName = "disconnectFromWlanScript";


        private ILogger Logger;

        private IConfiguration Configuration;

        public WifiConfigurator(ILoggerService logger, IConfiguration configuration) {
            Logger = logger.GetLogger<WifiConfigurator>();
            Configuration = configuration;
        }

        public bool ConnectToWlan(string essid, string secret) {
            Logger.Info($"[ConnectToWlan]Trying to connect to wlan wiht essid={essid}.");
            setScriptExecutionRights(changeWlanScriptName);

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

        public bool DisconnectFromWlan() {
            Logger.Info($"[DisconnectFromWlan]Disconnecting from current wlan.");
            setScriptExecutionRights(disconnectFromWlanScriptName);

            // disconnect from wlan
            executeCommand($"sudo ./{disconnectFromWlanScriptName}");

            // check if connected
            var isConnected = IsConnectedToWlan();

            return !isConnected;
        }

        public IEnumerable<string> GetAllWlans() {
            Logger.Info($"[GetAllWlans]Searching for reachable wifis.");
            var finalIds = new List<string>();

            try { 
                // get all wlans
                string command = $"sudo iwlist {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} scan | grep ESSID";
                string allWlans = string.Empty;
                console(command, (streamWriter, streamReader) => {
                    allWlans = streamReader.ReadToEnd();
                });

                // parse essids
                if (allWlans.Contains("ESSID")) {
                    var essids = allWlans.Split("ESSID:\"").ToList();
                    essids.RemoveAt(0);
                    foreach (var id in essids) {
                        finalIds.Add(id.Substring(0, id.IndexOf("\"")));
                    }
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

            // command : hostname -I | awk '{print $1}'
            try {
                // get all wlans
                string command = "hostname -I | awk '{print $1}'";
                string localIP = string.Empty;
                console(command, (streamWriter, streamReader) => {
                    localIP = streamReader.ReadToEnd();
                });

                localIP = localIP.Replace("\r", "").Replace("\n", "");

                // check if there is a local ip
                Logger.Trace($"[IsConnectedToWlan]localIP: {localIP}");
                IPAddress address;
                if (IPAddress.TryParse(localIP, out address)) {
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[IsConnectedToWlan]Exception while getting local ip.");
            }

            return false;
        }

        public bool CreateAP() {
            Logger.Info($"[CreateAP]Creating AP.");

            try {
                // create an access point without internet sharing and client isolation (clients can't talk to each other)
                string command = $"sudo create_ap --isolate-clients -n wlan0 {Configuration[ConfigurationVars.AP_SSID]} {Configuration[ConfigurationVars.AP_PASSPHRASE]}";
                bool error = false;
                executeCommand(command);
                //console(command, (streamWriter, streamReader) => {
                //    string outputText = streamReader.ReadToEnd();
                //    if (outputText.Contains("ERROR")) {
                //        error = true;
                //        Logger.Error($"[CreateAP]Error while creating ap: {outputText}");
                //    }
                //    else {
                //        Logger.Trace($"[CreateAP]create_ap output:\n" + outputText);
                //    }
                //});

                if (error) {
                    return false;
                }

                // start service
                string startServiceCommand = "sudo systemctl start create_ap";
                executeCommand(startServiceCommand);

                // verfiy that ap is active


                return true;
            } catch (Exception ex) {
                Logger.Error(ex, "[CreateAP]Exception while creating access point.");
            }

            return false;
        }

        public bool ShutdownAP() {
            Logger.Info($"[ShutdownAP]Shutting down AP.");

            try {
                // stop service
                string stopServiceCommand = "sudo systemctl stop create_ap";
                executeCommand(stopServiceCommand);
                //console(stopServiceCommand, (streamWriter, streamReader) => {
                //    string outputText = streamReader.ReadToEnd();
                //});

                // verfiy that ap is deactivated


                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[ShutdownAP]Exception while closing access point.");
            }

            return false;
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

        private static void console(string command, Action<StreamWriter, StreamReader> openedConsoleAction) {
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardInput = openedConsoleAction != null ? true : false,
                RedirectStandardOutput = openedConsoleAction != null ? true : false
            };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();

            openedConsoleAction?.Invoke(proc.StandardInput, proc.StandardOutput);

            proc.Close();
        }

        private static void executeCommand(string command) {
            console(command, null);
        }

        private static void setScriptExecutionRights(string scriptPath) {
            try {
                executeCommand($"chmod +x {scriptPath}");
            }
            catch {
            }
        }
    }
}
