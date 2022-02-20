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

            // Give the user time to be able to attach a debugger for example.
            Console.WriteLine("Press enter to start the test...");
            Console.ReadLine();

            logger.Info($"Starting app.");
            await rfCommunicator.Start();

            var moduleInfo = new ModuleInfoDto {
                ModuleId = 0x02
            };

            logger.Info($"Ping: {await rfCommunicator.PingModule(moduleInfo)}");

            logger.Info($"Battery Level: {await rfCommunicator.GetBatteryLevel(moduleInfo)}");

            var rfCommunicatorResult = await rfCommunicator.GetTempAndSoilMoisture(moduleInfo);
            if (rfCommunicatorResult.Success) {
                (double temp, double hum) = rfCommunicatorResult.Result as Tuple<double, double>;
                logger.Info($"Temp: {temp}; SoilMumid: {hum}");
            }
            else {
                logger.Error($"Failed to get the temperature and the soil moisture!");
            }

            logger.Info("Finished.");

            await rfCommunicator.Stop();

            logger.Info($"Stop finished.");
        }
    }
}
