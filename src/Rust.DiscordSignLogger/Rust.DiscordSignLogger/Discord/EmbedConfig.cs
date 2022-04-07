using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rust.SignLogger.Discord
{
    public class EmbedConfig
    {
        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Url")]
        public string Url { get; set; }

        [JsonProperty("Embed Color (Hex Color Code)")]
        public string Color { get; set; }

        [JsonProperty("Image Url")]
        public string Image { get; set; }

        [JsonProperty("Thumbnail Url")]
        public string Thumbnail { get; set; }

        [JsonProperty("Add Timestamp")]
        public bool Timestamp { get; set; }

        [JsonProperty(PropertyName = "Embed Fields")]
        public List<EmbedFieldConfig> Fields { get; set; }

        [JsonProperty("Footer")]
        public FooterConfig Footer { get; set; }

        public EmbedConfig(EmbedConfig settings)
        {
            Title = settings?.Title ?? "{server.name}";
            Description = settings?.Description ?? string.Empty;
            Url = settings?.Url ?? string.Empty;
            Color = settings?.Color ?? "#AC7061";
            Image = settings?.Image ?? "attachment://image.png";
            Thumbnail = settings?.Thumbnail ?? string.Empty;
            Timestamp = settings?.Timestamp ?? true;
            Fields = settings?.Fields ?? new List<EmbedFieldConfig>
            {
                new EmbedFieldConfig
                {
                    Title = "Player:",
                    Value = "{player.name} ([{player.id}](https://steamcommunity.com/profiles/{player.id}))",
                    Inline = true
                },
                new EmbedFieldConfig
                {
                    Title = "Owner:",
                    Value = "{dsl.entity.owner.name} ([{dsl.entity.owner.id}](https://steamcommunity.com/profiles/{dsl.entity.owner.id}))",
                    Inline = true
                },
                new EmbedFieldConfig
                {
                    Title = "Position:",
                    Value = "{dsl.entity.position:0.00!x} {dsl.entity.position:0.00!y} {dsl.entity.position:0.00!z}",
                    Inline = true
                },
                new EmbedFieldConfig
                {
                    Title = "Item:",
                    Value = "{dsl.entity.name}",
                    Inline = true
                },
                new EmbedFieldConfig
                {
                    Title = "Texture Index:",
                    Value = "{dsl.entity.textureindex}",
                    Inline = true
                }
            };
            Footer = new FooterConfig(settings?.Footer);
        }
    }
}