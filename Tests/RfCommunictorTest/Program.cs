using System;
using System.Threading.Tasks;
using GardeningSystem;
using GardeningSystem.Common.Models.DTOs;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Communication;

namespace RfCommunictorTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IoC.Init();

            var rfCommunicator = IoC.Get<IRfCommunicator>();
            var logger = IoC.Get<ILoggerService>().GetLogger<Program>();

            Console.WriteLine("IsTestEnviroment (in Configuration/settings.json) must be set to false!");

            // Give the user time to be able to attach a debugger for example.
            Console.WriteLine("Press enter to start the test...");
            Console.ReadLine();

            //logger.Info($"Starting app.");
            //await rfCommunicator.Start();

            var moduleInfo = await rfCommunicator.DiscoverNewModule(0x02);
            logger.Info($"Discover: {moduleInfo != null}");
            if (moduleInfo != null) {
                logger.Info($"Module is sensor: {moduleInfo.ModuleType == GardeningSystem.Common.Models.Enums.ModuleType.Sensor}");
            }
            else {
                // set it to an not existing module
                moduleInfo = new ModuleInfoDto {
                    ModuleId = 0x02
                };
            }

            logger.Info($"Ping: {(await rfCommunicator.PingModule(moduleInfo))?.Success ?? false}");

            var batLevel = await rfCommunicator.GetBatteryLevel(moduleInfo);
            if (batLevel.Success) {
                logger.Info($"Battery level: {Convert.ToSingle(batLevel.Result)}");
            }

            var rfCommunicatorResult = await rfCommunicator.GetTempAndSoilMoisture(moduleInfo);
            if (rfCommunicatorResult.Success) {
                (float temp, float hum) = (ValueTuple<float, float>) rfCommunicatorResult.Result;
                logger.Info($"Temp: {temp}; SoilMumid: {hum}");
            }

            logger.Info("Finished.");

            await rfCommunicator.Stop();

            logger.Info($"Stop finished.");
        }
    }
}
