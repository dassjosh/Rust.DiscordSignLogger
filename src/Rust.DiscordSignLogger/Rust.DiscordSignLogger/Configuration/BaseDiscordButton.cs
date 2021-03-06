using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;

namespace Rust.SignLogger.Configuration
{
    public class BaseDiscordButton
    {
        [JsonProperty(PropertyName = "Button Display Name")]
        public string DisplayName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "Button Style")]
        public ButtonStyle Style { get; set; }
        
        [JsonProperty(PropertyName = "Commands")]
        public List<string> Commands { get; set; }
        
        [JsonConstructor]
        public BaseDiscordButton()
        {
            
        }

        public BaseDiscordButton(BaseDiscordButton settings)
        {
            DisplayName = settings?.DisplayName ?? "Button Display Name";
            Style = settings?.Style ?? ButtonStyle.Primary;
            Commands = settings?.Commands ?? new List<string>();
        }
    }
}