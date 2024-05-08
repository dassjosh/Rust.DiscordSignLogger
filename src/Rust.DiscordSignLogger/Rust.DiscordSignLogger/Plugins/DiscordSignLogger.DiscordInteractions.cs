using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.State;
using Rust.SignLogger.Templates;

namespace Rust.SignLogger.Plugins;

public partial class DiscordSignLogger
{
    [DiscordMessageComponentCommand(CommandPrefix)]
    private void DiscordSignLoggerCommand(DiscordInteraction interaction)
    {
        if (!TryParseCommand(interaction.Data.CustomId, out TemplateKey messageId, out ButtonId buttonId, out SignUpdateState state))
        {
            SendErrorResponse(interaction, TemplateKeys.Errors.FailedToParse, GetPlaceholderData());
            return;
        }

        ImageButton button = _imageButtons[buttonId];
        if (button == null)
        {
            SendErrorResponse(interaction, TemplateKeys.Errors.ButtonIdNotFound, GetPlaceholderData().Add(PlaceholderDataKeys.ButtonId, buttonId.Id));
            return;
        }
        
        if (button.RequirePermissions && !UserHasButtonPermission(interaction, button))
        {
            SendTemplateResponse(interaction, TemplateKeys.NoPermission, GetPlaceholderData());
            return;
        }

        if (button.ConfirmModal)
        {
            ShowConfirmationModal(interaction, state, button, messageId, buttonId);
            return;
        }
        
        RunCommand(interaction, state, button, button.PlayerMessage, button.ServerMessage);
    }

    [DiscordMessageComponentCommand(ActionPrefix)]
    private void DiscordSignLoggerAction(DiscordInteraction interaction)
    {
        if (!TryParseCommand(interaction.Data.CustomId, out TemplateKey messageId, out ButtonId buttonId, out SignUpdateState state))
        {
            SendErrorResponse(interaction, TemplateKeys.Errors.FailedToParse, GetPlaceholderData());
            return;
        }

        SignMessage message = _signMessages[messageId];
        interaction.CreateResponse(Client, InteractionResponseType.UpdateMessage, new InteractionCallbackData
        {
            Components = CreateButtons(message, GetPlaceholderData(state, interaction), state.Serialize())
        });
    }

    [DiscordModalSubmit(ModalPrefix)]
    private void DiscordSignLoggerModal(DiscordInteraction interaction)
    {
        if (!TryParseCommand(interaction.Data.CustomId, out TemplateKey messageId, out ButtonId buttonId, out SignUpdateState state))
        {
            SendErrorResponse(interaction, TemplateKeys.Errors.FailedToParse, GetPlaceholderData());
            return;
        }

        ImageButton button = _imageButtons[buttonId];
        if (button == null)
        {
            SendErrorResponse(interaction, TemplateKeys.Errors.ButtonIdNotFound, GetPlaceholderData().Add(PlaceholderDataKeys.ButtonId, buttonId.Id));
            return;
        }

        string playerMessage = interaction.Data.GetComponent<InputTextComponent>(PlayerMessage).Value;
        string serverMessage = interaction.Data.GetComponent<InputTextComponent>(ServerMessage).Value;
        
        RunCommand(interaction, state, button, playerMessage, serverMessage);
    }
}