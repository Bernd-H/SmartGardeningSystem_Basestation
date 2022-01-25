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

    /// <inheritdoc/>
    /// <seealso cref="DbBaseRepository{T}"/>
    /// <seealso cref="DatabaseContext"/>
    public class SensorDataDbRepository : DbBaseRepository<ModuleData>, ISensorDataDbRepository {

        private ILogger Logger;

        public SensorDataDbRepository(ILoggerService loggerService) : base(loggerService) {
            Logger = loggerService.GetLogger<SensorDataDbRepository>();
        }

        /// <inheritdoc/>
        public async Task<bool> AddDataPoint(ModuleData data) {
            return await AddToTable(data) == 1;
        }


        /// <inheritdoc/>
        public async Task<bool> RemoveDataPoint(ModuleData data) {
            return await RemoveFromTable(data) == 1;
        }

        // Would update a table entry with the same sensor id and not with the ModuleData.uniqueDataPointId...
        //public async Task<bool> UpdateDataPoint(ModuleData updatedData) {
        //    return await UpdateObject(updatedData);
        //}

        /// <inheritdoc/>
        public async Task<IEnumerable<ModuleData>> GetAllDataPoints() {
            await LOCKER.WaitAsync();

            var dataPoints = context.sensordata.ToArray();

            LOCKER.Release();
            return dataPoints;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ModuleData>> QueryDataPointsById(Guid Id) {
            await LOCKER.WaitAsync();

            var dataPoints = context.sensordata.Where(u => u.Id == Id).ToArray();

            LOCKER.Release();
            return dataPoints;
        }
    }
}
