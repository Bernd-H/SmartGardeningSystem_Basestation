using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace GardeningSystem.Common.Configuration {

    /// <summary>
    /// Used to pass the settings file from GardeningSystem.Program.cs to StartupRestAPI.cs
    /// </summary>
    public static class ConfigurationContainer {

        private static IConfiguration configuration;
        public static IConfiguration Configuration {
            get {
                return GetConfigurationObject();
            }
        }

        public static string GetFullPath(string relativePath) {
            return new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName + "\\" + relativePath;
        }

        private static IConfiguration GetConfigurationObject() {
            if (configuration == null) {
                var GardeningSystemAssembly = Assembly.GetExecutingAssembly();
                var appSettingsFileStream = GardeningSystemAssembly.GetManifestResourceStream("GardeningSystem.Common.Configuration.settings.json");

                // load configuration
                var builder = new ConfigurationBuilder().AddJsonStream(appSettingsFileStream);
                //.AddJsonFile(appSettingsFilePath, optional: true, reloadOnChange: true);
                configuration = builder.Build();
            }

            return configuration;
        }
    }
}
