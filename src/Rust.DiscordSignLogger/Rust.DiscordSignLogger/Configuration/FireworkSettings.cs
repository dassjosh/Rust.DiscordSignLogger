using Newtonsoft.Json;

namespace Rust.DiscordSignLogger.Configuration
{
    public class FireworkSettings
    {
        [JsonProperty(PropertyName = "Image Size (Pixels)")]
        public int ImageSize { get; set; }
            
        [JsonProperty(PropertyName = "Circle Size (Pixels)")]
        public int CircleSize { get; set; }

        public FireworkSettings(FireworkSettings settings)
        {
            ImageSize = settings?.ImageSize ?? 250;
            CircleSize = settings?.CircleSize ?? 19;
        }
    }
}