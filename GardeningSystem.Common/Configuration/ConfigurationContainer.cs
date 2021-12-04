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
            return new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName + Path.DirectorySeparatorChar + relativePath;
        }

        private static IConfiguration GetConfigurationObject() {
            if (configuration == null) {
                //var GardeningSystemAssembly = Assembly.GetExecutingAssembly();
                //var appSettingsFileStream = GardeningSystemAssembly.GetManifestResourceStream("GardeningSystem.Common.Configuration.settings.json"); // for embedded resource
                string appSettingsFilePath = GetFullPath($"Configuration{Path.DirectorySeparatorChar}settings.json"); // for file

                // load configuration
                //var builder = new ConfigurationBuilder().AddJsonStream(appSettingsFileStream);
                var builder = new ConfigurationBuilder().AddJsonFile(appSettingsFilePath, optional: false, reloadOnChange: true);
                configuration = builder.Build();

                // changes some configurations if test environment is set
                OverwriteSomeSettings(configuration);
            }

            return configuration;
        }

        /// <summary>
        /// Overwrites some settings when test environment is set
        /// </summary>
        private static void OverwriteSomeSettings(IConfiguration conf) {
            if (conf[ConfigurationVars.IS_TEST_ENVIRONMENT] == "true") {
                //conf[ConfigurationVars.EXTERNALSERVER_DOMAIN] = "192.168.1.48";
                //conf[ConfigurationVars.EXTERNALSERVER_LOCALIP] = "192.168.1.48";

                // for local relay test
                //conf[ConfigurationVars.EXTERNALSERVER_DOMAIN] = "127.0.0.1";
                //conf[ConfigurationVars.EXTERNALSERVER_LOCALIP] = "127.0.0.1";
                //conf[ConfigurationVars.EXTERNALSERVER_APIPORT] = "5035";
                //conf[ConfigurationVars.EXTERNALSERVER_GETTOKEN_URL] = "http://{0}:{1}/api/Token/";
            }
        }
    }
}
