using Newtonsoft.Json;

namespace Rust.SignLogger.Configuration.PluginSupport;

public class PluginSettings
{
    [JsonProperty("Sign Artist Settings")] 
    public SignArtistSettings SignArtist { get; set; }

    [JsonConstructor]
    private PluginSettings() { }
    
    public PluginSettings(PluginSettings settings)
    {
        SignArtist = new SignArtistSettings(settings?.SignArtist);
    }
}