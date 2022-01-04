using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using GardeningSystem.Common.Specifications.Repositories.DB;
using GardeningSystem.DataAccess.Database;
using NLog;

namespace GardeningSystem.DataAccess.Repositories {
    public class SensorDataDbRepository : DbBaseRepository<ModuleData>, ISensorDataDbRepository {

        private ILogger Logger;

        public SensorDataDbRepository(ILoggerService loggerService) : base(loggerService) {
            Logger = loggerService.GetLogger<SensorDataDbRepository>();
        }

        public async Task<bool> AddDataPoint(ModuleData data) {
            return await AddToTable(data) == 1;
        }

        public async Task<bool> RemoveDataPoint(ModuleData data) {
            return await RemoveFromTable(data) == 1;
        }

        public async Task<bool> UpdateDataPoint(ModuleData updatedData) {
            return await UpdateObject(updatedData);
        }

        public async Task<IEnumerable<ModuleData>> GetAllDataPoints() {
            await LOCKER.WaitAsync();

            var dataPoints = context.sensordata.ToArray();

            LOCKER.Release();
            return dataPoints;
        }

        public async Task<IEnumerable<ModuleData>> QueryDataPointsById(Guid Id) {
            await LOCKER.WaitAsync();

            var dataPoints = context.sensordata.Where(u => u.Id == Id).ToArray();

            LOCKER.Release();
            return dataPoints;
        }
    }
}
