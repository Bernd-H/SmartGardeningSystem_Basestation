namespace GardeningSystem.Common.Configuration
{
    public static class ConfigurationVars
    {
        public static string MODULES_FILENAME = "modules_fileName";

        public static string RFAPP_FILENAME = "rfapp_fileName";

        public static string BASESTATION_GUID = "basestation_guid";

        public static string APPLICATIONSETTINGS_FILENAME = "applicationSettings_fileName";

        public static string WLANINTERFACE_NAME = "wlanInterface_name";

        // Cryptography
        // Server certificate
        public static string CERT_PRIVATEKEY_FILENAME = "cert_privateKey_fileName";
        public static string CERT_ISSUER = "cert_issuer";
        public static string CERT_SUBJECT = "cert_subject";
        

        // Communication
        // Local mobile app discovery
        public static string LOCALMOBILEAPPDISCOVERY_LISTENPORT = "localMobileAppDiscovery_listenPort";

        // Aes key exchange
        public static string AESKEYEXCHANGE_LISTENPORT = "aesKeyExchange_listenPort";

        // Command listener
        public static string COMMANDLISTENER_LISTENPORT = "commandListener_listenPort";

        // WanManager
        public static string WANMANAGER_CONNECTIONSERVICEPORT = "wanManager_connectionServicePort";

        // External Server API
        public static string EXTERNALSERVER_DOMAIN = "externalServer_domain";
        public static string EXTERNALSERVER_LOCALIP = "externalServer_localIp";
        public static string EXTERNALSERVER_APIPORT = "externalServer_apiPort";
        public static string EXTERNALSERVER_USER_URL = "externalServer_userUrl";
        public static string EXTERNALSERVER_GETTOKEN_URL = "externalServer_getTokenUrl";
        public static string EXTERNALSERVER_IPLOOKUP_URL = "externalServer_ipLookupUrl";


        // GardeningSystem.RestAPI
        // Authentication
        public static string ISSUER_SIGNINGKEY = "rest_api_jwt:key";
        public static string ISSUER = "rest_api_jwt:issuer";

        // Services
        public static string WATERINGJOB_ENABLED = "wateringJob_enabled";
        public static string COMMUNICATIONJOB_ENABLED = "communicationJob_enabled";
        public static string PUBLICIPJOB_ENABLED = "publicIpJob_enabled";

        // Test Environment
        public static string IS_TEST_ENVIRONMENT = "isTestEnvironment";
    }
}
