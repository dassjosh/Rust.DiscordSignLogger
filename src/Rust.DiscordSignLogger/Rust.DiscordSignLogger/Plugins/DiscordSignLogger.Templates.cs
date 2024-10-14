using System.Collections.Generic;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.Templates;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=12
public partial class DiscordSignLogger
{
    public void RegisterTemplates()
    {
        HashSet<string> messages = new();
        foreach (SignMessage message in _pluginConfig.SignMessages)
        {
            if (messages.Add(message.MessageId.Name))
            {
                _templates.RegisterGlobalTemplateAsync(this, message.MessageId, CreateDefaultTemplate(),
                    new TemplateVersion(1, 0, 2), new TemplateVersion(1, 0, 2));
            }
            else
            {
                PrintWarning($"Duplicate Message ID: '{message.MessageId.Name}'. Please check your config and correct the duplicate Sign Message ID's");
            }
        }
        
        _templates.RegisterGlobalTemplateAsync(this, TemplateKeys.Action.Message, CreateActionMessage($"{DefaultKeys.User.Mention} ran command \"{PlaceholderKeys.Command}\"", DiscordColor.Blurple), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _buttonTemplates.RegisterGlobalTemplateAsync(this, TemplateKeys.Action.Button, new ButtonTemplate("Actions", ButtonStyle.Primary, BuildCustomId(ActionPrefix, PlaceholderKeys.MessageId, null, PlaceholderKeys.MessageState)), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        
        RegisterEn();
        RegisterRu();
    }

    public void RegisterEn()
    {
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.NoPermission, CreateMessage("You do not have permission to perform this action", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.FailedToParse, CreateMessage("An error occurred parsing button data", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.ButtonIdNotFound, CreateMessage($"Failed to find button with id: {PlaceholderKeys.ButtonId}. Please validate the button exists in the config.", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Block.Success, CreateMessage($"{DefaultKeys.Player.Name} ({DefaultKeys.Player.Id}) has been sign blocked for {DefaultKeys.Timespan.Formatted}.", DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Block.Errors.PlayerNotFound, CreateMessage($"Failed to find player with id: {PlaceholderKeys.PlayerId}", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Block.Errors.IsAlreadyBanned, CreateMessage($"{DefaultKeys.Player.Name} ({DefaultKeys.Player.Id}) is already banned", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Unblock.Success, CreateMessage($"You have removed {DefaultKeys.Player.Name} ({DefaultKeys.Player.Id}) sign block.", DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Unblock.Errors.PlayerNotFound, CreateMessage($"Failed to find player with id: {PlaceholderKeys.PlayerId}", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Unblock.Errors.NotBanned, CreateMessage($"{DefaultKeys.Player.Name} ({DefaultKeys.Player.Id}) is not sign blocked.", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
    }

    public void RegisterRu()
    {
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.NoPermission, CreateMessage("У вас нет разрешения на выполнение этого действия", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0), "ru");
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.FailedToParse, CreateMessage("Произошла ошибка при анализе данных кнопки.", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0), "ru");
        _templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Errors.ButtonIdNotFound, CreateMessage($"Не удалось найти кнопку с идентификатором: {PlaceholderKeys.ButtonId}. Пожалуйста, проверьте наличие кнопки в файле конфигурации.", DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0), "ru");
    }

    public DiscordMessageTemplate CreateMessage(string description, DiscordColor color)
    {
        return new DiscordMessageTemplate
        {
            Embeds = new List<DiscordEmbedTemplate>
            {
                CreateEmbedTemplate(description, color)
            }
        };
    }

    public DiscordMessageTemplate CreateActionMessage(string description, DiscordColor color)
    {
        return new DiscordMessageTemplate
        {
            Embeds = new List<DiscordEmbedTemplate>
            {
                CreateEmbedTemplate(description, color)
            },
            Components = new List<BaseComponentTemplate>
            {
                new ButtonTemplate("Image Message", ButtonStyle.Link, "discord://-/channels/{guild.id}/{channel.id}/{message.id}")
            }
        };
    }
    
    public DiscordEmbedTemplate CreateEmbedTemplate(string description, DiscordColor color)
    {
        return new()
        {
            Description = $"[{Title}] {description}",
            Color = color.ToHex(),
            Footer = GetFooterTemplate()
        };
    }
    
    public DiscordMessageTemplate CreateDefaultTemplate()
    {
        return new DiscordMessageTemplate
        {
            Embeds = new List<DiscordEmbedTemplate>
            {
                new()
                {
                    Title = $"{DefaultKeys.Server.Name}",
                    Color = "#AC7061",
                    ImageUrl = "attachment://image.png",
                    TimeStamp = true,
                    Fields = new List<DiscordEmbedFieldTemplate>
                    {
                        new()
                        {
                            Name = "Player:",
                            Value = $"{DefaultKeys.Player.Name} ([{DefaultKeys.Player.Id}]({DefaultKeys.Player.SteamProfile}))",
                            Inline = true
                        },
                        new()
                        {
                            Name = "Owner:",
                            Value = $"{PlaceholderKeys.OwnerKeys.Name} ([{PlaceholderKeys.OwnerKeys.Id}]({PlaceholderKeys.OwnerKeys.SteamProfile}))",
                            Inline = true
                        },
                        new()
                        {
                            Name = "Position:",
                            Value = $"{PlaceholderKeys.Position}",
                            Inline = true
                        },
                        new()
                        {
                            Name = "Item:",
                            Value = $"{PlaceholderKeys.EntityName}",
                            Inline = true
                        },
                        new()
                        {
                            Name = "Is Outside:",
                            Value = $"{PlaceholderKeys.IsOutside}",
                            Inline = true
                        },
                        new()
                        {
                            Name = "Texture Index",
                            Value = $"{PlaceholderKeys.TextureIndex}",
                            Inline = true,
                            HideIfEmpty = true
                        },
                        new()
                        {
                            Name = "Sign Artist URL",
                            Value = $"{PlaceholderKeys.SignArtistUrl}",
                            Inline = true,
                            HideIfEmpty = true
                        }
                    },
                    Footer = GetFooterTemplate()
                }
            }
        };
    }

    public EmbedFooterTemplate GetFooterTemplate()
    {
        return new EmbedFooterTemplate
        {
            Enabled = true,
            Text = $"{DefaultKeys.Plugin.Name} V{DefaultKeys.Plugin.Version} by {DefaultKeys.Plugin.Author}",
            IconUrl = "https://assets.umod.org/images/icons/plugin/61f1b7f6da7b6.png"
        };
    }
}