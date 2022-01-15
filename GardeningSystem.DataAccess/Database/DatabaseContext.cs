using GardeningSystem.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardeningSystem.DataAccess.Database {
    public class DatabaseContext : DbContext {

        // DbSet names must be lower case. Else there are problems on linux

        public DbSet<ModuleData> sensordata { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder) {
            //string connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=";
            string connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=hmufckmlmycj";
            dbContextOptionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}
