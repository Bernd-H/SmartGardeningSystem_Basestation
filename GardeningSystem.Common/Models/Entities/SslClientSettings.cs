namespace GardeningSystem.Common.Models.Entities {
    public class SslClientSettings : ClientSettings {

        /// <summary>
        /// The name of the server that shares this System.Net.Security.SslStream.
        /// </summary>
        public string TargetHost { get; set; }
    }
}
