using System;
using System.Threading;
using GardeningSystem;
using GardeningSystem.Common.Specifications;
using GardeningSystem.DataAccess;
using NLog;

namespace LinuxCommandsTests
{
    public class Program
    {
        static void Main(string[] args)
        {
            IoC.Init();
            var wifiConfigurator = IoC.Get<IWifiConfigurator>();
            var logger = IoC.Get<ILoggerService>().GetLogger<Program>();

            try {
                //CheckIsConnectedToWlanMethod(logger, wifiConfigurator);
                //return;

                var wlans = wifiConfigurator.GetAllWlans();
                foreach (var wlan in wlans) {
                    logger.Info($"Wlan found: {wlan}");
                }

                logger.Info($"Has internet = {wifiConfigurator.HasInternet()}.");

                logger.Info($"Is connected to wlan = {wifiConfigurator.IsConnectedToWlan()}");

                #region change wifi
                //logger.Info($"Connect to a wlan test.");
                logger.Info($"Enter wlan ssid and passphrase for later.");
                Console.Write("SSID: ");
                string ssid = Console.ReadLine();
                logger.Info($"Entered ssid={ssid}");
                Console.Write("Pwd: ");
                string pwd = Console.ReadLine();
                logger.Info($"Entered pwd={pwd}");
                //bool wentGood = wifiConfigurator.ConnectToWlan(ssid, pwd);
                //logger.Info($"Wlan change went good: {wentGood}");
                #endregion

                Thread.Sleep(1000);

                #region test access point methods
                logger.Info($"Disconnecting form wifi: {wifiConfigurator.DisconnectFromWlan()}");
                logger.Info($"Creating access point..: {wifiConfigurator.CreateAP()}");
                //Console.WriteLine("Press enter to change wlan...");
                //logger.Info($"Enter pressed: {Console.ReadLine()}");
                //Thread.Sleep(1000);
                logger.Info($"Is connected to wlan = {wifiConfigurator.IsConnectedToWlan()}");
                logger.Info($"Waiting 30 seconds.");
                Thread.Sleep(30 * 1000);
                logger.Info($"Closing access point: {wifiConfigurator.ShutdownAP()}");
                logger.Info($"Connecting to wifi again: {wifiConfigurator.ChangeWlan(ssid, pwd)}");
                #endregion

                //logger.Info($"Shutting down system.");
                //WifiConfigurator.executeCommand("sudo shutdown -h now");

                Console.WriteLine("Finished!");
            }
            catch (Exception ex) {
                logger.Fatal(ex);
            }
            finally {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        static void CheckIsConnectedToWlanMethod(ILogger logger, IWifiConfigurator wifiConfigurator) {
            logger.Info($"Is connected to wlan = {wifiConfigurator.IsConnectedToWlan()}");

            logger.Info($"Enter wlan ssid and passphrase for later.");
            Console.Write("SSID: ");
            string ssid = Console.ReadLine();
            logger.Info($"Entered ssid={ssid}");
            Console.Write("Pwd: ");
            string pwd = Console.ReadLine();
            logger.Info($"Entered pwd={pwd}");

            logger.Info($"Disconnecting form wifi: {wifiConfigurator.DisconnectFromWlan()}");

            Thread.Sleep(30 * 1000);

            logger.Info($"Is connected to wlan = {wifiConfigurator.IsConnectedToWlan()}");

            logger.Info($"Connecting to wifi again: {wifiConfigurator.ChangeWlan(ssid, pwd)}");

            logger.Info($"CheckIsConnectedToWlanMethod() finished.");
        }
    }
}
