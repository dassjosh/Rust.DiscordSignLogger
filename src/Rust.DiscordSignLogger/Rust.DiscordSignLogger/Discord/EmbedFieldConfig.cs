using Newtonsoft.Json;

namespace Rust.SignLogger.Discord
{
    public class EmbedFieldConfig
    {
        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("Value")]
        public string Value { get; set; }

        [JsonProperty("Inline")]
        public bool Inline { get; set; }
    }
}