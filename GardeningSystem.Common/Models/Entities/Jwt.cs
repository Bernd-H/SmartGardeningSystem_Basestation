using Newtonsoft.Json;

namespace GardeningSystem.Common.Models.Entities {
    public class Jwt {

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
