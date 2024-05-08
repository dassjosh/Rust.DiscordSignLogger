using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rust.SignLogger.Enums;

namespace Rust.SignLogger.Configuration;

public class ReplaceImageSettings
{
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(PropertyName = "Replaced Mode (None, Url, Text)")]
    public EraseMode Mode { get; set; }
            
    [JsonProperty(PropertyName = "URL")]
    public string Url { get; set; }
            
    [JsonProperty(PropertyName = "Message")]
    public string Message { get; set; }
            
    [JsonProperty(PropertyName = "Font Size")]
    public int FontSize { get; set; }
            
    [JsonProperty(PropertyName = "Text Color")]
    public string TextColor { get; set; }
            
    [JsonProperty(PropertyName = "Body Color")]
    public string BodyColor { get; set; }

    [JsonConstructor]
    private ReplaceImageSettings() { }
    
    public ReplaceImageSettings(ReplaceImageSettings settings)
    {
            Mode = settings?.Mode ?? EraseMode.Url;
            Url = settings?.Url ?? "https://i.postimg.cc/mD5xZ5R5/Erased-4.png";
            Message = settings?.Message ?? "ERASED BY ADMIN";
            FontSize = settings?.FontSize ?? 16;
            TextColor = settings?.TextColor ?? "#cd4632";
            BodyColor = settings?.BodyColor ?? "#000000";
    }
    
    private bool ShouldSerializeUrl() => Mode == EraseMode.Url;
    private bool ShouldSerializeMessage() => Mode == EraseMode.Text;
    private bool ShouldSerializeFontSize() => Mode == EraseMode.Text;
    private bool ShouldSerializeTextColor() => Mode == EraseMode.Text;
    private bool ShouldSerializeBodyColor() => Mode == EraseMode.Text;
}