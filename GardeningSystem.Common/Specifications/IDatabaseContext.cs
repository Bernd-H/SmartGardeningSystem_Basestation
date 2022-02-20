using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GardeningSystem.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardeningSystem.Common.Specifications {

    /// <summary>
    /// Inherits from DbContext.
    /// Class that contains the connection string for the mysql database and multiple tabels as DbSet instance.
    /// </summary>
    public interface IDatabaseContext {

        /// <summary>
        /// Table where the sensor measurements get stored.
        /// </summary>
        DbSet<ModuleData> sensordata { get; set; }
    }
}
