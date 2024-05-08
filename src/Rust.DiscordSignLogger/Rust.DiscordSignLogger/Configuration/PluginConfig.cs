using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Logging;
using Rust.SignLogger.Configuration.PluginSupport;

namespace Rust.SignLogger.Configuration;

public class PluginConfig
{
    [DefaultValue("")]
    [JsonProperty(PropertyName = "Discord Bot Token")]
    public string DiscordApiKey { get; set; }

    [DefaultValue(true)]
    [JsonProperty(PropertyName = "Disable Discord Button After Use")]
    public bool DisableDiscordButton { get; set; }
            
    [JsonProperty(PropertyName = "Action Log Channel ID")]
    public Snowflake ActionLogChannel { get; set; }
            
    [JsonProperty(PropertyName = "Replace Erased Image (Requires SignArtist)")]
    public ReplaceImageSettings ReplaceImage { get; set; }

    [JsonProperty(PropertyName = "Firework Settings")]
    public FireworkSettings FireworkSettings { get; set; }
            
    [JsonProperty(PropertyName = "Sign Messages")]
    public List<SignMessage> SignMessages { get; set; }
        
    [JsonProperty(PropertyName = "Buttons")]
    public List<ImageButton> Buttons { get; set; }
    
    public PluginSettings PluginSettings { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    [DefaultValue(DiscordLogLevel.Info)]
    [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
    public DiscordLogLevel ExtensionDebugging { get; set; } = DiscordLogLevel.Info;
}