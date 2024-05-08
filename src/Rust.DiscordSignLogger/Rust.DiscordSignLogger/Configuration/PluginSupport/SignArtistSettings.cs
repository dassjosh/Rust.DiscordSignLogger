using Newtonsoft.Json;

namespace Rust.SignLogger.Configuration.PluginSupport;

public class SignArtistSettings
{
    [JsonProperty("Log /sil")] 
    public bool LogSil { get; set; }

    [JsonProperty("Log /sili")] 
    public bool LogSili { get; set; }

    [JsonProperty("Log /silt")] 
    public bool LogSilt { get; set; }

    [JsonConstructor]
    private SignArtistSettings() { }
    
    public SignArtistSettings(SignArtistSettings settings)
    {
        LogSil = settings?.LogSil ?? true;
        LogSili = settings?.LogSili ?? true;
        LogSilt = settings?.LogSilt ?? true;
    }

    public bool ShouldLog(string url)
    {
        if (url.StartsWith("http://assets.imgix.net"))
        {
            return LogSilt;
        }

        if (ItemManager.itemDictionaryByName.ContainsKey(url))
        {
            return LogSili;
        }

        return LogSil;
    }
}