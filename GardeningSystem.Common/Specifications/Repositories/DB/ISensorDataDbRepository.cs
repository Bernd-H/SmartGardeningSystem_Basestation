using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Repositories.DB {
    public interface ISensorDataDbRepository {

        Task<bool> AddDataPoint(ModuleData data);

        Task<bool> RemoveDataPoint(ModuleData data);

        Task<bool> UpdateDataPoint(ModuleData updatedData);

        Task<IEnumerable<ModuleData>> GetAllDataPoints();

        Task<IEnumerable<ModuleData>> QueryDataPointsById(Guid Id);
    }
}
