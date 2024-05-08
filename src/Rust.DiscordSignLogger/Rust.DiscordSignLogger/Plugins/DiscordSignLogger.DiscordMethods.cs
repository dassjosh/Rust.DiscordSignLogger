using System;
using System.Collections.Generic;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.State;
using Rust.SignLogger.Templates;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=7
public partial class DiscordSignLogger
{
    public void SendDiscordMessage(BaseImageUpdate update)
    {
        SignUpdateState state = new(update);
            
        StateKey encodedState = state.Serialize();
        
        using PlaceholderData data = GetPlaceholderData(state);
        data.ManualPool();
        data.AddPlayer(state.Player)
            .Add(PlaceholderDataKeys.State, state)
            .Add(PlaceholderDataKeys.Owner, state.Owner)
            .Add(PlaceholderDataKeys.MessageState, encodedState);

        if (update is SignageUpdate signage)
        {
            data.Add(PlaceholderDataKeys.SignArtistUrl, signage.Url);
        }
        
        for (int index = 0; index < _pluginConfig.SignMessages.Count; index++)
        {
            SignMessage signMessage = _pluginConfig.SignMessages[index];
            DiscordMessageTemplate message = _templates.GetGlobalTemplate(this, signMessage.MessageId);
            MessageCreate create = message.ToMessage<MessageCreate>(data);
            data.Add(PlaceholderDataKeys.MessageId, signMessage.MessageId);

            create.AddAttachment("image.png", update.GetImage(), "image/png", $"{update.DisplayName} Updated {update.Entity.ShortPrefabName} @{update.Entity.transform.position} On {DateTime.Now:f}");

            if (signMessage.Buttons.Count != 0)
            {
                if (signMessage.UseActionButton)
                {
                    create.Components = new List<ActionRowComponent>
                    {
                        new()
                        {
                            Components = { _buttonTemplates.GetGlobalTemplate(this, TemplateKeys.Action.Button).ToComponent(data) }
                        }
                    };
                }
                else
                {
                    create.Components = CreateButtons(signMessage, data, encodedState);
                }
            }
            
            signMessage.MessageChannel?.CreateMessage(Client, create);
        }
    }

    private List<ActionRowComponent> CreateButtons(SignMessage signMessage, PlaceholderData data, StateKey encodedState)
    {
        MessageComponentBuilder builder = new();
        for (int i = 0; i < signMessage.Buttons.Count; i++)
        {
            ButtonId buttonId = signMessage.Buttons[i];
            ImageButton command = _imageButtons[buttonId];
            if (command.Commands.Count == 0)
            {
                continue;
            }

            if (command.Style == ButtonStyle.Link)
            {
                builder.AddLinkButton(command.DisplayName, _placeholders.ProcessPlaceholders(command.Commands[0], data));
            }
            else
            {
                builder.AddActionButton(command.Style, command.DisplayName, BuildCustomId(CommandPrefix, signMessage.MessageId, buttonId, encodedState));
            }
        }

        return builder.Build();
    }

    private string BuildCustomId(string command, IDiscordKey messageId, ButtonId? buttonId, IDiscordKey encodedState)
    {
        return $"{command} {messageId.ToString()} {(buttonId.HasValue ? buttonId.Value.Id : "_")} {encodedState}";
    }
}