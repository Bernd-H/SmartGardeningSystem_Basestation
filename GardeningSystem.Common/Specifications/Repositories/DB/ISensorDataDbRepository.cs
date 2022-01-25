using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;

namespace GardeningSystem.Common.Specifications.Repositories.DB {

    /// <summary>
    /// Repository that saves and loads ModuleData objects in an table in the database.
    /// </summary>
    public interface ISensorDataDbRepository {

        /// <summary>
        /// Saves a new data point.
        /// </summary>
        /// <param name="data">Datapoint to save.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains
        /// a boolean that is true, when the operation got completed successfully.</returns>
        Task<bool> AddDataPoint(ModuleData data);

        /// <summary>
        /// Deletes a already saved data point.
        /// </summary>
        /// <param name="data">Datapoint to remove.</param>
        /// <returns>A task that represents the asynchronous remove operation. The task result contains
        /// a boolean that is true, when the operation got completed successfully.</returns>
        Task<bool> RemoveDataPoint(ModuleData data);

        /// <summary>
        /// Updates a already existing data point.
        /// </summary>
        /// <param name="updatedData">Updated datapoint, that has the same ModuleData.uniqueDataPointId as the stored one.</param>
        /// <returns>A task that represents the asynchronous update operation. The task result contains
        /// a boolean that is true, when the operation got completed successfully.</returns>
        //Task<bool> UpdateDataPoint(ModuleData updatedData);

        /// <summary>
        /// Gets all stored data points from the database table.
        /// </summary>
        /// <returns>A task that represents the asynchronous get operation. The task result contains
        /// a list with all entires of the database table.</returns>
        Task<IEnumerable<ModuleData>> GetAllDataPoints();

        /// <summary>
        /// Gets all stored data points that have the same sensor id. (ModuleData.Id)
        /// </summary>
        /// <param name="Id">Id of the sensor.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains
        /// a list of all table entries that have the same sensor id as <paramref name="Id"/>.</returns>
        Task<IEnumerable<ModuleData>> QueryDataPointsById(Guid Id);
    }
}
