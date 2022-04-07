using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Builders.MessageComponents;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Configuration.ActionLog;
using Rust.SignLogger.Data;
using Rust.SignLogger.Lang;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=6
    public partial class DiscordSignLogger
    {
        private void HandleMessageComponentCommand(DiscordInteraction interaction)
        {
            if (!interaction.Data.ComponentType.HasValue || interaction.Data.ComponentType.Value != MessageComponentType.Button)
            {
                return;
            }

            string buttonId = interaction.Data.CustomId;
            if (!buttonId.StartsWith(CommandPrefix))
            {
                return;
            }

            SignUpdateLog log = _pluginData.GetLog(interaction.Message.Id);
            if (log == null)
            {
                DisableAllButtons(interaction.Message);
                SendComponentUpdateResponse(interaction);
                CreateResponse(interaction,  Lang(LangKeys.DeletedLog, null, _pluginConfig.DeleteLogDataAfter));
                return;
            }

            buttonId = buttonId.Replace(CommandPrefix, string.Empty);
            int hash;
            if (!int.TryParse(buttonId, out hash))
            {
                DisableAllButtons(interaction.Message);
                SendComponentUpdateResponse(interaction);
                CreateResponse(interaction, Lang(LangKeys.DeletedLog, null, _pluginConfig.DeleteButtonCacheAfter));
                return;
            }

            ImageMessageButtonCommand command = _buttonData.Get(hash);
            if (command == null)
            {
                DisableAllButtons(interaction.Message);
                SendComponentUpdateResponse(interaction);
                CreateResponse(interaction, Lang(LangKeys.DeletedLog, null, _pluginConfig.DeleteButtonCacheAfter));
                return;
            }

            RunCommand(interaction, log, command);
        }

        public void RunCommand(DiscordInteraction interaction, SignUpdateLog data, ImageMessageButtonCommand button)
        {
            try
            {
                _log = data;
                _activeMember = interaction.Member;
                _interaction = interaction;

                if (button.RequirePermissions && !UserHasButtonPermission(interaction, button))
                {
                    CreateResponse(interaction, Lang(LangKeys.NoPermission));
                    return;
                }

                IPlayer logPlayer = data.Player;

                _actions.Clear();
                
                foreach (string buttonCommand in button.Commands)
                {
                    _sb.Clear();
                    _sb.Append(buttonCommand);
                    ParsePlaceholders(logPlayer, _sb);
                    string serverCommand = _sb.ToString();
                    covalence.Server.Command(serverCommand);
                    
                    if (_actionChannel != null)
                    {
                        if (_actions.Length != 0)
                        {
                            _actions.AppendLine();
                        }
                        _actions.Append(Lang(LangKeys.ActionMessage));
                        ParsePlaceholders(logPlayer, _actions);
                        _actions.Replace("{dsl.command}", serverCommand);
                    }
                }

                if (_actionChannel != null)
                {
                    _actionMessage.Content = _actions.ToString();
                    if (_pluginConfig.ActionLog.Buttons.Count != 0)
                    {
                        MessageComponentBuilder builder = new MessageComponentBuilder();
                        for (int index = 0; index < _pluginConfig.ActionLog.Buttons.Count; index++)
                        {
                            ActionMessageButtonCommand actionButton = _pluginConfig.ActionLog.Buttons[index];
                            if (actionButton.Commands.Count == 0)
                            {
                                continue;
                            }
                            
                            if (actionButton.Style == ButtonStyle.Link)
                            {
                                builder.AddLinkButton(actionButton.DisplayName, ParsePlaceholders(logPlayer, actionButton.Commands[0]));
                            }
                        }

                        _actionMessage.Components = builder.Build();
                    }
                    
                    _actionChannel.CreateMessage(_client, _actionMessage);
                }

                if (!string.IsNullOrEmpty(button.PlayerMessage))
                {
                    BasePlayer player = logPlayer.Object as BasePlayer;
                    if (player != null && player.IsConnected)
                    {
                        Chat(player, button.PlayerMessage);
                    }
                }

                if (!string.IsNullOrEmpty(button.ServerMessage))
                {
                    covalence.Server.Broadcast(button.ServerMessage);
                }
                
                if (_pluginConfig.DisableDiscordButton)
                {
                    DisableButton(interaction.Message, interaction.Data.CustomId);
                }
            
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                    Data = new InteractionCallbackData
                    {
                        Components = interaction.Message.Components
                    }
                });
            }
            finally
            {
                _log = null;
                _activeMember = null;
                _interaction = null;
            }
        }

        public bool UserHasButtonPermission(DiscordInteraction interaction, ImageMessageButtonCommand button)
        {
            for (int index = 0; index < button.AllowedRoles.Count; index++)
            {
                Snowflake role = button.AllowedRoles[index];
                if (interaction.Member.HasRole(role))
                {
                    return true;
                }
            }
            
            IPlayer player = interaction.Member.User.Player;
            for (int index = 0; index < button.AllowedGroups.Count; index++)
            {
                string group = button.AllowedGroups[index];
                if (permission.UserHasGroup(player.Id, group))
                {
                    return true;
                }
            }

            return false;
        }

        public void DisableButton(DiscordMessage message, string id)
        {
            for (int index = 0; index < message.Components.Count; index++)
            {
                ActionRowComponent row = message.Components[index];
                for (int i = 0; i < row.Components.Count; i++)
                {
                    BaseComponent component = row.Components[i];
                    if (component is ButtonComponent)
                    {
                        ButtonComponent button = (ButtonComponent)component;
                        if (button.CustomId == id)
                        {
                            button.Disabled = true;
                            return;
                        }
                    }
                }
            }
        }
        
        public void DisableAllButtons(DiscordMessage message)
        {
            for (int index = 0; index < message.Components.Count; index++)
            {
                ActionRowComponent row = message.Components[index];
                for (int i = 0; i < row.Components.Count; i++)
                {
                    BaseComponent component = row.Components[i];
                    if (component is ButtonComponent)
                    {
                        ButtonComponent button = (ButtonComponent)component;
                        button.Disabled = true;
                    }
                }
            }
        }
        
        private void SendComponentUpdateResponse(DiscordInteraction interaction)
        {
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.UpdateMessage,
                Data = new InteractionCallbackData
                {
                    Components = interaction.Message.Components
                }
            });
        }
        
        public void CreateResponse(DiscordInteraction interaction, string response)
        {
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = response,
                    Flags = MessageFlags.Ephemeral
                }
            });
        }
    }
}