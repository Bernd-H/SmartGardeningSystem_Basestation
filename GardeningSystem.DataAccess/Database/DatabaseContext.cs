using System;
using System.Collections.Generic;
using GardeningSystem.Common.Configuration;
using GardeningSystem.Common.Models.Entities;
using GardeningSystem.Common.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

namespace GardeningSystem.DataAccess.Database {

    /// <inheritdoc/>
    public class DatabaseContext : DbContext, IDatabaseContext {

        // DbSet names must be lower case. Else there are problems on linux

        /// <inheritdoc/>
        public DbSet<ModuleData> sensordata { get; set; }


        private IConfiguration Configuration;

        public DatabaseContext() {
            // Mock IConfiguration
            var configBuilder = new ConfigurationBuilder();
            var configList = new List<KeyValuePair<string, string>>();
            configList.Add(new KeyValuePair<string, string>(ConfigurationVars.IS_TEST_ENVIRONMENT, "true"));
            configBuilder.AddInMemoryCollection(configList);

            Configuration = configBuilder.Build();
        }

        public DatabaseContext(IConfiguration configuration) {
            Configuration = configuration;
        }

        /// <inheritdoc/>
        protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder) {
            string connectionString;
            if (Convert.ToBoolean(Configuration[ConfigurationVars.IS_TEST_ENVIRONMENT])) {
                connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=";
            }
            else {
                connectionString = "Data Source=localhost;Initial Catalog=smartgardening_basestation;User ID=root;Password=hmufckmlmycj";
            }

            dbContextOptionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}
