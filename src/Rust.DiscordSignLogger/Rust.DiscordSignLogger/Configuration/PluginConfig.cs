using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Logging;
using Rust.SignLogger.Configuration.ActionLog;

namespace Rust.SignLogger.Configuration
{
    public class PluginConfig
    {
        [DefaultValue("")]
        [JsonProperty(PropertyName = "Discord Bot Token")]
        public string DiscordApiKey { get; set; }
        
        [JsonProperty(PropertyName = "Action Log Settings")]
        public ActionLogConfig ActionLog { get; set; }

        [DefaultValue(true)]
        [JsonProperty(PropertyName = "Disable Discord Button After User")]
        public bool DisableDiscordButton { get; set; }
            
        [DefaultValue(14)]
        [JsonProperty(PropertyName = "Delete Saved Log Data After (Days)")]
        public float DeleteLogDataAfter { get; set; }
            
        [DefaultValue(14)]
        [JsonProperty(PropertyName = "Delete Cached Button Data After (Days)")]
        public float DeleteButtonCacheAfter { get; set; }
            
        [JsonProperty(PropertyName = "Replace Erased Image (Requires SignArtist)")]
        public ReplaceImageSettings ReplaceImage { get; set; }

        [JsonProperty(PropertyName = "Firework Settings")]
        public FireworkSettings FireworkSettings { get; set; }
            
        [JsonProperty(PropertyName = "Sign Messages")]
        public List<SignMessage> SignMessages { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(DiscordLogLevel.Info)]
        [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
        public DiscordLogLevel ExtensionDebugging { get; set; } = DiscordLogLevel.Info;
    }
}