using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.State;
using Rust.SignLogger.Templates;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=6
public partial class DiscordSignLogger
{
    public void RunCommand(DiscordInteraction interaction, SignUpdateState state, ImageButton button, string playerMessage, string serverMessage)
    {
        using PlaceholderData data = GetPlaceholderData(state, interaction)
            .AddGuild(Client, interaction.GuildId)
            .Add(PlaceholderDataKeys.PlayerMessage, playerMessage)
            .Add(PlaceholderDataKeys.ServerMessage, serverMessage);
        
        data.ManualPool();

        _sb.Clear();
        foreach (string buttonCommand in button.Commands)
        {
            string command = _placeholders.ProcessPlaceholders(buttonCommand, data);
            covalence.Server.Command(command);
                    
            if (_actionChannel != null)
            {
                _sb.AppendLine(command);
            }
        }

        if (_actionChannel != null)
        {
            string command = _sb.ToString();
            data.Add(PlaceholderDataKeys.Command, command);
            _actionChannel.CreateGlobalTemplateMessage(Client, TemplateKeys.Action.Message, null, data);
        }

        if (!string.IsNullOrEmpty(playerMessage))
        {
            BasePlayer player = state.Player.Object as BasePlayer;
            if (player != null && player.IsConnected)
            {
                string message = _placeholders.ProcessPlaceholders(playerMessage, data);
                Chat(player, message);
            }
        }

        if (!string.IsNullOrEmpty(serverMessage))
        {
            string message = _placeholders.ProcessPlaceholders(serverMessage, data);
            covalence.Server.Broadcast(message);
        }
                
        if (_pluginConfig.DisableDiscordButton)
        {
            DisableButton(interaction.Message, interaction.Data.CustomId);
        }
            
        interaction.CreateResponse(Client, new InteractionResponse
        {
            Type = InteractionResponseType.UpdateMessage,
            Data = new InteractionCallbackData
            {
                Components = interaction.Message.Components
            }
        });
    }

    public void ShowConfirmationModal(DiscordInteraction interaction, SignUpdateState state, ImageButton button, TemplateKey messageId, ButtonId buttonId)
    {
        InteractionModalBuilder builder = new(interaction);
        builder.AddModalCustomId(BuildCustomId(ModalPrefix, messageId, buttonId, state.Serialize()));
        builder.AddModalTitle(button.DisplayName);
        builder.AddInputText(PlayerMessage, "Player Message", InputTextStyles.Paragraph, button.PlayerMessage, false);
        builder.AddInputText(ServerMessage, "Server Message", InputTextStyles.Paragraph, button.ServerMessage, false);
        interaction.CreateResponse(Client, builder);
    }

    public bool UserHasButtonPermission(DiscordInteraction interaction, ImageButton button)
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
        if (player != null)
        {
            for (int index = 0; index < button.AllowedGroups.Count; index++)
            {
                string group = button.AllowedGroups[index];
                if (permission.UserHasGroup(player.Id, group))
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    public bool TryParseCommand(string command, out TemplateKey messageId, out ButtonId buttonId, out SignUpdateState state)
    {
        messageId = default;
        buttonId = default;
        state = null;
        
        ReadOnlySpan<char> span = command.AsSpan();
        ReadOnlySpan<char> token = " ";
        
        //Command Prefix can be ignored
        if (!span.TryParseNextString(token, out span, out ReadOnlySpan<char> _)) return false;
        if (!span.TryParseNextString(token, out span, out ReadOnlySpan<char> messageIdString)) return false;
        if (!span.TryParseNextString(token, out span, out ReadOnlySpan<char> buttonIdString)) return false;
        if (!span.TryParseNextString(token, out span, out ReadOnlySpan<char> stateString)) return false;

        messageId = new TemplateKey(messageIdString.ToString());
        buttonId = new ButtonId(buttonIdString.ToString());
        state = SignUpdateState.Deserialize(stateString);
        return true;
    }

    public void DisableButton(DiscordMessage message, string id)
    {
        for (int index = 0; index < message.Components.Count; index++)
        {
            ActionRowComponent row = message.Components[index];
            for (int i = 0; i < row.Components.Count; i++)
            {
                BaseComponent component = row.Components[i];
                if (component is ButtonComponent button && button.CustomId == id)
                {
                    button.Disabled = true;
                    return;
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
                if (component is ButtonComponent button)
                {
                    button.Disabled = true;
                }
            }
        }
    }

    public void SendErrorResponse(DiscordInteraction interaction, TemplateKey template, PlaceholderData data)
    {
        DisableAllButtons(interaction.Message);
        SendComponentUpdateResponse(interaction);
        SendFollowupResponse(interaction, template, data);
    }
        
    public void SendComponentUpdateResponse(DiscordInteraction interaction)
    {
        interaction.CreateResponse(Client, new InteractionResponse
        {
            Type = InteractionResponseType.UpdateMessage,
            Data = new InteractionCallbackData
            {
                Components = interaction.Message.Components
            }
        });
    }

    public void SendTemplateResponse(DiscordInteraction interaction, TemplateKey templateName, PlaceholderData data)
    {
        interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, templateName, null, data);
    }
    
    public void SendFollowupResponse(DiscordInteraction interaction, TemplateKey templateName, PlaceholderData data)
    {
        interaction.CreateFollowUpTemplateResponse(Client, templateName, null, data);
    }
    
    public IEnumerable<IPlayer> GetBannedPlayers()
    {
        foreach (ulong key in _pluginData.SignBannedUsers.Keys)
        {
            IPlayer player = FindPlayerById(StringCache<ulong>.Instance.ToString(key));
            if (player != null)
            {
                yield return player;
            }
        }
    }
}