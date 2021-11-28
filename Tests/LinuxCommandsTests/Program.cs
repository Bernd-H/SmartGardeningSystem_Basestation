using System;
using GardeningSystem;
using GardeningSystem.Common.Specifications;

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

            Console.WriteLine("Connect to a new wlan test:");
            logger.Info($"Connect to a wlan test.");
            Console.Write("SSID: ");
            string ssid = Console.ReadLine();
            logger.Info($"Entered ssid={ssid}");
            Console.Write("Pwd: ");
            string pwd = Console.ReadLine();
            logger.Info($"Entered pwd={pwd}");
            bool wentGood = wifiConfigurator.ConnectToWlan(ssid, pwd);
            logger.Info($"Wlan change went good: {wentGood}");

            Console.WriteLine("Finished!");
            Console.ReadLine();
        }
    }
}
