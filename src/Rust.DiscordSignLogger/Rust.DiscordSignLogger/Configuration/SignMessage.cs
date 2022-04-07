using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Rust.SignLogger.Discord;

namespace Rust.SignLogger.Configuration
{
    public class SignMessage
    {
        [JsonProperty("Discord Channel ID")]
        public Snowflake ChannelId { get; set; }

        [JsonProperty(PropertyName = "Message Config")]
        public MessageConfig MessageConfig { get; set; }
        
        [JsonProperty("Button Commands")]
        public List<ImageMessageButtonCommand> Commands { get; set; }
        
        [JsonIgnore]
        public DiscordChannel MessageChannel;

        public SignMessage(SignMessage settings)
        {
            ChannelId = settings?.ChannelId ?? default(Snowflake);
            Commands = settings?.Commands ?? new List<ImageMessageButtonCommand>
            {
                new ImageMessageButtonCommand
                {
                    DisplayName = "Player Profile",
                    Style = ButtonStyle.Link,
                    Commands = new List<string> { "https://steamcommunity.com/profiles/{player.id}" },
                    PlayerMessage = string.Empty,
                    ServerMessage = string.Empty,
                    RequirePermissions = false,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Owner Profile",
                    Style = ButtonStyle.Link,
                    Commands = new List<string> { "https://steamcommunity.com/profiles/{dsl.entity.owner.id}" },
                    PlayerMessage = string.Empty,
                    ServerMessage = string.Empty,
                    RequirePermissions = false,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Erase",
                    Style = ButtonStyle.Primary,
                    Commands = new List<string> { "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                    PlayerMessage = "An admin erased your sign for being inappropriate",
                    ServerMessage = string.Empty,
                    RequirePermissions = false,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Sign Block (24 Hours)",
                    Style = ButtonStyle.Primary,
                    Commands = new List<string> { "dsl.signblock {player.id} 1440.0" },
                    PlayerMessage = "An admin erased your sign for being inappropriate",
                    ServerMessage = string.Empty,
                    RequirePermissions = true,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Kill Entity",
                    Style = ButtonStyle.Secondary,
                    Commands = new List<string> { "entid kill {dsl.entity.id}" },
                    PlayerMessage = "An admin killed your sign for being inappropriate",
                    ServerMessage = string.Empty,
                    RequirePermissions = true,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Kick Player",
                    Style = ButtonStyle.Danger,
                    Commands = new List<string> { "kick {player.id} \"{dsl.kick.reason}\"", "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                    PlayerMessage = string.Empty,
                    ServerMessage = string.Empty,
                    RequirePermissions = true,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                },
                new ImageMessageButtonCommand
                {
                    DisplayName = "Ban Player",
                    Style = ButtonStyle.Danger,
                    Commands = new List<string> { "ban {player.id} \"{dsl.ban.reason}\"", "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                    PlayerMessage = string.Empty,
                    ServerMessage = string.Empty,
                    RequirePermissions = true,
                    AllowedRoles = new List<Snowflake>(),
                    AllowedGroups = new List<string>()
                }
            };
            MessageConfig = new MessageConfig(settings?.MessageConfig);
        }
    }
}