using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;

namespace Rust.DiscordSignLogger.Configuration.ActionLog
{
    public class ActionLogConfig
    {
        [JsonProperty(PropertyName = "Channel ID")]
        public Snowflake Channel { get; set; }
        
        [JsonProperty(PropertyName = "Buttons")]
        public List<ActionMessageButtonCommand> Buttons { get; set; }

        public ActionLogConfig(ActionLogConfig settings)
        {
            Channel = settings?.Channel ?? default(Snowflake);
            Buttons = settings?.Buttons ?? new List<ActionMessageButtonCommand>
            {
                new ActionMessageButtonCommand
                {
                    DisplayName = "Image Message",
                    Style = ButtonStyle.Link,
                    Commands = new List<string> { "discord://-/channels/{dsl.action.guild.id}/{dsl.action.channel.id}/{dsl.action.message.id}" }
                }
            };
        }
    }
}