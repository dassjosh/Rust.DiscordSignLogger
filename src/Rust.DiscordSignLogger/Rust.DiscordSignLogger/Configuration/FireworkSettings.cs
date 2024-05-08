using Newtonsoft.Json;

namespace Rust.SignLogger.Configuration;

public class FireworkSettings
{
    [JsonProperty(PropertyName = "Image Size (Pixels)")]
    public int ImageSize { get; set; }

    [JsonProperty(PropertyName = "Circle Size (Pixels)")]
    public int CircleSize { get; set; }

    [JsonConstructor]
    private FireworkSettings() { }
    
    public FireworkSettings(FireworkSettings settings)
    {
        ImageSize = settings?.ImageSize ?? 250;
        CircleSize = settings?.CircleSize ?? 19;
    }
}