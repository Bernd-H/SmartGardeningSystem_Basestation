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

            await Task.Delay(5000);

            logger.Info($"Starting app.");
            await rfCommunicator.Start();

            var moduleInfo = new ModuleInfoDto {
                ModuleId = 0x02
            };

            await Task.Delay(5000);

            logger.Info($"Ping: {await rfCommunicator.PingModule(moduleInfo)}");

            await Task.Delay(5000);

            logger.Info($"Battery Level: {await rfCommunicator.GetBatteryLevel(moduleInfo)}");

            await Task.Delay(5000);

            (double temp, double hum) = await rfCommunicator.GetTempAndSoilMoisture(moduleInfo);

            logger.Info($"Temp: {temp}; SoilMumid: {hum}");

            logger.Info("Finished.");

            await rfCommunicator.Stop();

            logger.Info($"Stop finished.");
        }
    }
}
