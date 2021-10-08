namespace GardeningSystem.Common.Configuration
{
    public static class ConfigurationVars
    {
        public static string MODULES_FILENAME = "modules_fileName";

        public static string RFAPP_FILENAME = "rfapp_fileName";

        public static string BASESTATION_GUID = "basestation_guid";

        public static string APPLICATIONSETTINGS_FILENAME = "applicationSettings_fileName";

        public static string WLANINTERFACE_NAME = "wlanInterface_name";


        // GardeningSystem.RestAPI
        // Authentication
        public static string ISSUER_SIGNINGKEY = "rest_api_jwt:key";
        public static string ISSUER = "rest_api_jwt:issuer";

        // Services
        public static string WATERINGJOB_ENABLED = "wateringJob_enabled";

        // Test Environment
        public static string IS_TEST_ENVIRONMENT = "isTestEnvironment";
    }
}
