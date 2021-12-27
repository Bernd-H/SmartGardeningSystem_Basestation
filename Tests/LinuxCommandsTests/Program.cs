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

            #region test access point methods
            logger.Info($"Disconnecting form wifi: {wifiConfigurator.DisconnectFromWlan()}");
            logger.Info($"Creating access point..: {wifiConfigurator.CreateAP()}");
            logger.Info($"Waiting 30 seconds.");
            Thread.Sleep(30 * 1000);
            logger.Info($"Closing access point: {wifiConfigurator.ShutdownAP()}");
            logger.Info($"Connecting to wifi again: {wifiConfigurator.ConnectToWlan(ssid, pwd)}");
            #endregion

            logger.Info($"Shutting down system.");
            WifiConfigurator.executeCommand("sudo shutdown -h now");

            Console.WriteLine("Finished!");
        }
    }
}
