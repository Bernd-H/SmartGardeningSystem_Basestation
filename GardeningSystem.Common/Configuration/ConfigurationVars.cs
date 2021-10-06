namespace GardeningSystem.Common.Configuration
{
    public static class ConfigurationVars
    {
        public static string MODULES_FILENAME = "modules_fileName";

        public static string RFAPP_FILEPATH = "rfapp_path";

        public static string BASESTATION_GUID = "basestation_guid";

        public static string APPLICATIONSETTINGS_FILEPATH = "applicationSettings_path";

        public static string WLANINTERFACE_NAME = "wlanInterface_name";


        // GardeningSystem.RestAPI
        // Authentication
        public static string ISSUER_SIGNINGKEY = "rest_api_jwt:key";
        public static string ISSUER = "rest_api_jwt:issuer";
    }
}
