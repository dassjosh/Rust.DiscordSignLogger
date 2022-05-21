using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;
using Rust.SignLogger.Plugins;

namespace Rust.SignLogger.Configuration
{
    public class ImageMessageButtonCommand : BaseDiscordButton
    {
        [JsonProperty(PropertyName = "Player Message")]
        public string PlayerMessage { get; set; }

        [JsonProperty(PropertyName = "Server Message")]
        public string ServerMessage { get; set; }
        
        [JsonProperty(PropertyName = "Requires Permissions To Use Button")]
        public bool RequirePermissions { get; set; }
        
        [JsonProperty(PropertyName = "Allowed Discord Roles (Role ID)")]
        public List<Snowflake> AllowedRoles { get; set; }
        
        [JsonProperty(PropertyName = "Allowed Oxide Groups (Group Name)")]
        public List<string> AllowedGroups { get; set; }

        [JsonIgnore]
        public int CommandId { get; private set; }

        [JsonIgnore]
        public string CommandCustomId { get; private set; }

        [JsonConstructor]
        public ImageMessageButtonCommand()
        {
            
        }
        
        public ImageMessageButtonCommand(ImageMessageButtonCommand settings) : base(settings)
        {
            PlayerMessage = settings?.PlayerMessage ?? "Player Message";
            ServerMessage = settings?.ServerMessage ?? "Server Message";
            RequirePermissions = settings?.RequirePermissions ?? true;
            AllowedRoles = settings?.AllowedRoles ?? new List<Snowflake>();
            AllowedGroups = settings?.AllowedGroups ?? new List<string>();
        }

        public void SetCommandId()
        {
            CommandId = GetCommandId();
            CommandCustomId = $"{DiscordSignLogger.CommandPrefix}{CommandId}";
        }

        public int GetCommandId()
        {
            unchecked
            {
                int commandId = 0;
                if (Commands.Count != 0)
                {
                    commandId = StringComparer.OrdinalIgnoreCase.GetHashCode(Commands[0]);
                }
                
                for (int index = 1; index < Commands.Count; index++)
                {
                    string command = Commands[index];
                    commandId = (commandId * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(command);
                }

                return commandId;
            }
        }
    }
}