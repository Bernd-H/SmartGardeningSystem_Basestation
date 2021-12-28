using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Utilities;
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

        public bool ManagedConnectToWlan(string ssid, string secret) {
            if (IsAccessPointUp()) {
                Logger.Info($"[ManagedConnectToWlan]Shutting down the access point: {ShutdownAP()}.");
            }

            return ChangeWlan(ssid, secret);
        }

        public bool ChangeWlan(string essid, string secret) {
            Logger.Info($"[ConnectToWlan]Trying to connect to wlan wiht essid={essid}.");
            setScriptExecutionRights(changeWlanScriptName);

            // connect to wlan
            executeCommand($"sudo ./{changeWlanScriptName} \"{essid}\" \"{secret}\"");

            // check if connected
            return loopFunction(IsConnectedToWlan, 2000, 30);
        }

        public bool DisconnectFromWlan() {
            Logger.Info($"[DisconnectFromWlan]Disconnecting from current wlan.");
            setScriptExecutionRights(disconnectFromWlanScriptName);

            // disconnect from wlan
            executeCommand($"sudo ./{disconnectFromWlanScriptName}");

            // check if connected
            return loopFunction(() => !IsConnectedToWlan(), 2000, 30);
        }

        public IEnumerable<WlanInfo> GetAllWlans() {
            Logger.Info($"[GetAllWlans]Searching for reachable wifis.");
            var finalIds = new List<WlanInfo>();

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
                        finalIds.Add(new WlanInfo {
                            Ssid = id.Substring(0, id.IndexOf("\""))
                        });
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
            Logger.Trace($"[IsConnectedToWlan]Checking wifi connection.");

            try {
                var myIp = getLocalIpThroughTerminal();
                Logger.Info($"myIP: " + myIp?.ToString());
                if (myIp == null || !myIp.IsPrivateAddress()) {
                    return false;
                }

                var networkIpAddress = myIp.GetNetworkAddress(IpUtils.GetSubnetMask(myIp));
                var firstAddressInTheNetwork = networkIpAddress.GetNextIPAddress();
                Logger.Info($"FirstIP: " + firstAddressInTheNetwork.ToString());

                // check if first address in the network is our ip address
                // if so -> we are probably the router (access point is up)
                if (firstAddressInTheNetwork.Equals(myIp)) {
                    return false;
                }
                else {
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[IsConnectedToWlan]Exception while getting/processing local ip.");
            }

            return false;
        }

        public bool CreateAP() {
            Logger.Info($"[CreateAP]Creating AP.");

            try {
                // create an access point without internet sharing and client isolation (clients can't talk to each other)
                string command = $"sudo create_ap --isolate-clients -n {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} " +
                    $"{Configuration[ConfigurationVars.AP_SSID]} {Configuration[ConfigurationVars.AP_PASSPHRASE]}";
                bool error = false;
                executeCommand(command);

                if (error) {
                    return false;
                }

                // start service
                string startServiceCommand = "sudo systemctl start create_ap";
                executeCommand(startServiceCommand);

                // verfiy that ap is active
                return loopFunction(IsAccessPointUp, 6000, 10);
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

                // verfiy that ap is deactivated
                return loopFunction(() => !IsAccessPointUp(), 2000, 30);
            }
            catch (Exception ex) {
                Logger.Error(ex, "[ShutdownAP]Exception while closing access point.");
            }

            return false;
        }

        public bool IsAccessPointUp() {
            Logger.Trace($"[IsAccessPointUp]Checking mode of interface {Configuration[ConfigurationVars.WLANINTERFACE_NAME]}.");

            try {
                // stop service
                string command = "iwconfig wlan0";
                string outputText = string.Empty;
                console(command, (streamWriter, streamReader) => {
                    outputText = streamReader.ReadToEnd();
                });

                if (outputText.Contains("Mode:")) {
                    string textStartingWithMode = outputText.Substring(outputText.IndexOf("Mode:")+5);
                    if (textStartingWithMode.Substring(0, textStartingWithMode.IndexOf(" ")) == "Master") {
                        return true;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[IsAccessPointUp]Exception while getting/processing interface state.");
            }

            return false;
        }

        /// <summary>
        /// Calls every <paramref name="millisecondsInterval"/> ms a function (<paramref name="func"/>).
        /// Stops when the function returns true, or <paramref name="maxLoopCount"/> is reached.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="millisecondsInterval"></param>
        /// <param name="maxLoopCount"></param>
        /// <returns></returns>
        private bool loopFunction(Func<bool> func, int millisecondsInterval, int maxLoopCount) {
            bool success = false;
            int attempt = 0;
            do {
                if (attempt > 0) {
                    Thread.Sleep(millisecondsInterval);
                }

                success = func();
                attempt++;
            } while (!success && attempt <= maxLoopCount);

            return success;
        }

        private string extractIpFromString(string s) {
            var regex = new Regex(@"([0-9]{1,3}\.){3}[0-9]{1,3}", RegexOptions.Compiled);

            var match = regex.Match(s);
            if (!match.Success) return null;

            return match.Value;
        }

        private IPAddress getLocalIpThroughTerminal() {
            Logger.Trace($"[getLocalIpThroughTerminal]Getting local ip address.");

            try {
                // get all wlans
                string command = "hostname -I | awk '{print $1}'";
                string localIP = string.Empty;
                console(command, (streamWriter, streamReader) => {
                    localIP = streamReader.ReadToEnd();
                });

                localIP = extractIpFromString(localIP);

                IPAddress address = null;
                IPAddress.TryParse(localIP, out address);
                return address;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[getLocalIpThroughTerminal]Exception while getting local ip.");
            }

            return null;
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

        public static void executeCommand(string command) {
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
