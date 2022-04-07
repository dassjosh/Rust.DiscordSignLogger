using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rust.SignLogger.Discord
{
    public class MessageConfig
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("embeds")]
        public List<EmbedConfig> Embeds { get; set; }

        public MessageConfig(MessageConfig settings)
        {
            Content = settings?.Content ?? string.Empty;
            Embeds = settings?.Embeds ?? new List<EmbedConfig> { new EmbedConfig(null) };
        }
    }
}