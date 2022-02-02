using Newtonsoft.Json;

namespace Rust.DiscordSignLogger.Discord
{
    public class FooterConfig
    {
        [JsonProperty("Icon Url")]
        public string IconUrl { get; set; }

        [JsonProperty("Text")]
        public string Text { get; set; }

        [JsonProperty("Enabled")]
        public bool Enabled { get; set; }

        public FooterConfig(FooterConfig settings)
        {
            IconUrl = settings?.IconUrl ?? string.Empty;
            Text = settings?.Text ?? string.Empty;
            Enabled = settings?.Enabled ?? true;
        }
    }
}