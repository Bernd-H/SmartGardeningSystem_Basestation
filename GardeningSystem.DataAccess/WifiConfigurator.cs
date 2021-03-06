using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Utilities;
using Microsoft.Extensions.Configuration;
using NLog;

namespace GardeningSystem.DataAccess {

    /// <inheritdoc/>
    public class WifiConfigurator : IWifiConfigurator {

        static string changeWlanScriptName = "changeWlanScript";

        static string disconnectFromWlanScriptName = "disconnectFromWlanScript";

        /// <inheritdoc/>
        public bool AccessPointStarted { get; private set; } = false;


        private Process _accessPointProcess = null;

        private StringBuilder _accessPointProcessOutput = new StringBuilder();



        private ILogger Logger;

        private IConfiguration Configuration;

        public WifiConfigurator(ILoggerService logger, IConfiguration configuration) {
            Logger = logger.GetLogger<WifiConfigurator>();
            Configuration = configuration;
        }

        /// <inheritdoc/>
        public bool ManagedConnectToWlan(string ssid, string secret) {
            if (IsAccessPointUp()) {
                Logger.Info($"[ManagedConnectToWlan]Shutting down the access point: {ShutdownAP()}.");
            }

            return ChangeWlan(ssid, secret);
        }

        /// <inheritdoc/>
        public bool ChangeWlan(string essid, string secret) {
            Logger.Info($"[ConnectToWlan]Trying to connect to wlan wiht essid={essid}.");
            setScriptExecutionRights(changeWlanScriptName);

            // connect to wlan
            executeCommand($"sudo ./{changeWlanScriptName} \"{essid}\" \"{secret}\"");

            // check if connected
            return loopFunction(IsConnectedToWlan, 500, 20);
            //return true;
        }

        /// <inheritdoc/>
        public bool DisconnectFromWlan() {
            if (!AccessPointStarted) {
                Logger.Info($"[DisconnectFromWlan]Disconnecting from current wlan.");
                //setScriptExecutionRights(disconnectFromWlanScriptName);

                // disconnect from wlan
                //executeCommand($"sudo ./{disconnectFromWlanScriptName}");

                // check if connected
                //return loopFunction(() => !IsConnectedToWlan(), 2000, 30);
                
                return ChangeWlan("xfdh466533467d", "xfdh466533467d");
            }
            else {
                return true;
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public bool HasInternet() {
            Logger.Trace($"[HasInternet]Checking internet connection.");
            return pingHost("google.com");
        }

        /// <inheritdoc/>
        public bool IsConnectedToWlan() {
            Logger.Trace($"[IsConnectedToWlan]Checking wifi connection.");

            try {
                var myIp = getLocalIpThroughTerminal();
                if (myIp == null || !myIp.IsPrivateAddress()) {
                    return false;
                }

                var networkIpAddress = myIp.GetNetworkAddress(IpUtils.GetSubnetMask(myIp));
                var firstAddressInTheNetwork = networkIpAddress.GetNextIPAddress();

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

        /// <inheritdoc/>
        public bool CreateAP() {
            try {
                Logger.Info($"[CreateAP]Starting up the access point...");

                executeCommand("sudo systemctl start create_ap.service");
                AccessPointStarted = true;
                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[CreateAP]Exception while creating access point.");
            }

            return false;
        }

        /// <inheritdoc/>
        public bool ShutdownAP() {
            try {
                Logger.Info($"[ShutdownAP]Shutting down the access point.");

                executeCommand("sudo systemctl stop create_ap.service");
                AccessPointStarted = false;
                return true;
            }
            catch (Exception ex) {
                Logger.Error(ex, "[ShutdownAP]Exception while closing access point.");
            }

            return false;
        }

        /// <inheritdoc/>
        public bool IsAccessPointUp() {
            return AccessPointStarted;
        }

        #region Old access point methods

        [Obsolete]
        public bool CreateAP_old() {
            if (_accessPointProcess != null && !_accessPointProcess.HasExited) {
                Logger.Info($"[CreateAP]Process is already running.");
                return false;
            }

            try {
                Logger.Info($"[CreateAP]Starting up the access point...");
                _accessPointProcessOutput = _accessPointProcessOutput.Clear();

                // create an access point without internet sharing and client isolation (clients can't talk to each other)
                string command = $"sudo create_ap --isolate-clients -n {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} " +
                    $"{Configuration[ConfigurationVars.AP_SSID]} {Configuration[ConfigurationVars.AP_PASSPHRASE]}";

                var process = consoleV2(command, new DataReceivedEventHandler((sender, e) => {
                    Logger.Trace($"[CreateAP-consoleV2]: {e.Data}");
                    if (!String.IsNullOrEmpty(e.Data)) {
                        _accessPointProcessOutput.Append("\n" + e.Data);
                    }
                }));

                // verfiy that ap is active
                var accessPointIsUp = loopFunction(IsAccessPointUp, 30000, 3);
                if (accessPointIsUp) {
                    _accessPointProcess = process;
                }

                return accessPointIsUp;
            } catch (Exception ex) {
                Logger.Error(ex, "[CreateAP]Exception while creating access point.");
            }

            return false;
        }

        [Obsolete]
        public bool ShutdownAP_old() {
            try {
                if (_accessPointProcess != null && !_accessPointProcess.HasExited) {
                    Logger.Info($"[ShutdownAP]Shutting down the access point.");

                    // get pid
                    string pid = extractPidFromString(_accessPointProcessOutput.ToString());
                    Logger.Info($"[ShutdownAP]Pid of the access point process: {pid}");

                    // shutdown ap
                    executeCommand($"sudo kill -INT {pid}");
                    _accessPointProcess.Close();
                    _accessPointProcess = null;

                    // verfiy that ap is deactivated
                    //return loopFunction(() => !IsAccessPointUp(), 30000, 3);
                    return true;
                }
                else {
                    Logger.Info($"[ShutdownAP]Access point is inactive.");
                    return true;
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[ShutdownAP]Exception while closing access point.");
            }

            return false;
        }

        [Obsolete]
        public bool IsAccessPointUp_old() {
            Logger.Trace($"[IsAccessPointUp]Checking mode of interface {Configuration[ConfigurationVars.WLANINTERFACE_NAME]}.");

            try {
                // stop service
                string command = "iwconfig wlan0";
                string outputText = string.Empty;
                console(command, (streamWriter, streamReader) => {
                    outputText = streamReader.ReadToEnd();
                });

                if (outputText.Contains("Mode:")) {
                    string textStartingWithMode = outputText.Substring(outputText.IndexOf("Mode:") + 5);
                    string mode = textStartingWithMode.Substring(0, textStartingWithMode.IndexOf(" "));
                    Logger.Info($"[IsAccessPointUp]Interface {Configuration[ConfigurationVars.WLANINTERFACE_NAME]} mode: {mode}.");
                    if (mode == "Master") {
                        return true;
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, "[IsAccessPointUp]Exception while getting/processing interface state.");
            }

            return false;
        }

        #endregion

        /// <inheritdoc/>
        public void ReloadDaemon() {
            Logger.Info($"[ReloadDaemon]Sending reload command.");
            executeCommand($"sudo systemctl reload-or-restart {Configuration[ConfigurationVars.DAEMON_NAME]}.service");
        }

        public void RebootSystem() {
            Logger.Info($"[RebootSystem]Sending the reboot command.");
            executeCommand($"sudo reboot");
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

        private string extractPidFromString(string s) {
            var regex = new Regex(@"PID: [0-9]{1,10}", RegexOptions.Compiled);

            var match = regex.Match(s);
            if (!match.Success) return null;

            var pid = match.Value;

            return pid.Substring(5);
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

        private void console(string command, Action<StreamWriter, StreamReader> openedConsoleAction) {
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardInput = openedConsoleAction != null ? true : false,
                RedirectStandardOutput = openedConsoleAction != null ? true : false
            };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();

            openedConsoleAction?.Invoke(proc.StandardInput, proc.StandardOutput);

            Logger.Trace($"[console]Waiting till command \"{command}\" has finished.");
            proc.WaitForExit();
            Logger.Trace($"[console]Command \"{command}\" has finished.");
            proc.Close();
        }


        /// <summary>
        /// Does not wait for close the started process.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="dataReceivedEventHandler"></param>
        /// <returns></returns>
        private Process consoleV2(string command, DataReceivedEventHandler dataReceivedEventHandler) {
            ProcessStartInfo startInfo = new ProcessStartInfo() {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = true
            };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();

            if (dataReceivedEventHandler != null) {
                proc.OutputDataReceived += dataReceivedEventHandler;

                // Asynchronously read the standard output of the spawned process. 
                // This raises OutputDataReceived events for each line of output.
                proc.BeginOutputReadLine();
            }

            return proc;
        }

        private void executeCommand(string command) {
            console(command, null);
        }

        private void setScriptExecutionRights(string scriptPath) {
            try {
                executeCommand($"chmod +x {scriptPath}");
            }
            catch {
            }
        }
    }
}
