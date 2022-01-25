using GardeningSystem.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardeningSystem.DataAccess.Database {

    /// <summary>
    /// Inherits from DbContext.
    /// Class that contains the connection string for the mysql database and multiple tabels as DbSet instance.
    /// </summary>
    public class DatabaseContext : DbContext {

        // DbSet names must be lower case. Else there are problems on linux

        /// <summary>
        /// Table where the sensor measurements get stored.
        /// </summary>
        public DbSet<ModuleData> sensordata { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder) {
            //string connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=";
            string connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=hmufckmlmycj";
            dbContextOptionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}
