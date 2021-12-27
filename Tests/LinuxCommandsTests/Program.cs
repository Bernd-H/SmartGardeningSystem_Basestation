using System;
using GardeningSystem;
using GardeningSystem.Common.Specifications;
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
            Console.WriteLine("Connect to a new wlan test:");
            logger.Info($"Connect to a wlan test.");
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
            //logger.Info($"Disconnecting form wifi: {wifiConfigurator.DisconnectFromWlan()}");
            //logger.Info($"Creating access point..: {wifiConfigurator.CreateAP()}");
            //logger.Info($"----------------------------------------------");
            //GetAllWifis(wifiConfigurator, logger);
            //logger.Info($"----------------------------------------------");
            //logger.Info($"Waiting 2*60 seconds.");
            //Thread.Sleep(60 * 2 * 1000);
            logger.Info($"Closing access point: {wifiConfigurator.ShutdownAP()}");

            bool wentGood = wifiConfigurator.ConnectToWlan(ssid, pwd);
            logger.Info($"Connecting to wifi again: {wentGood}");
            #endregion

            Console.WriteLine("Finished!");
            //Console.ReadLine();
        }


        private static void GetAllWifis(IWifiConfigurator wifiConfigurator, ILogger logger) {
            var wlans = wifiConfigurator.GetAllWlans();
            foreach (var wlan in wlans) {
                logger.Info($"Wlan found: {wlan}");
            }
        }
    }
}
