using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Ids;

namespace Rust.SignLogger.Configuration;

public class SignMessage
{
    [JsonProperty("Message ID")]
    public TemplateKey MessageId { get; set; }
    
    [JsonProperty("Discord Channel ID")]
    public Snowflake ChannelId { get; set; }

    [JsonProperty("Use Action Button")] 
    public bool UseActionButton { get; set; } = true;
        
    [JsonProperty("Buttons")]
    public List<ButtonId> Buttons { get; set; }
        
    [JsonIgnore]
    public DiscordChannel MessageChannel;

    [JsonConstructor]
    private SignMessage() { }
    
    public SignMessage(SignMessage settings)
    {
        MessageId = settings?.MessageId ?? new TemplateKey("DEFAULT");
        if (!MessageId.IsValid)
        {
            MessageId = new TemplateKey("DEFAULT");
        }
        ChannelId = settings?.ChannelId ?? default(Snowflake);
        UseActionButton = settings?.UseActionButton ?? true;
        Buttons = settings?.Buttons ?? new List<ButtonId>
        {
            new("ERASE"),
            new("SIGN_BLOCK_24_HOURS"),
            new("KILL_ENTITY"),
            new("KICK_PLAYER"),
            new("BAN_PLAYER"),
        };
    }
}