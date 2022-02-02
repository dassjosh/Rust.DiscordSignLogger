using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Builders.MessageComponents;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Rust.DiscordSignLogger.Configuration;
using Rust.DiscordSignLogger.Data;
using Rust.DiscordSignLogger.Discord;
using Rust.DiscordSignLogger.Lang;
using Rust.DiscordSignLogger.Updates;

namespace Rust.DiscordSignLogger.Plugins
{
    //Define:FileOrder=7
    public partial class DiscordSignLogger
    {
        public void SendDiscordMessage(BaseImageUpdate update)
        {
            try
            {
                _log = update;
                SignUpdateLog log = new SignUpdateLog(update);
                for (int index = 0; index < update.Messages.Count; index++)
                {
                    SignMessage signMessage = update.Messages[index];
                    MessageConfig message = signMessage.MessageConfig;

                    MessageCreate create = new MessageCreate();
                    if (!string.IsNullOrEmpty(message.Content))
                    {
                        create.Content = ParsePlaceholders(null, message.Content);
                    }

                    if (message.Embeds.Count != 0)
                    {
                        create.Embeds = new List<DiscordEmbed>(message.Embeds.Count);
                        foreach (EmbedConfig config in message.Embeds)
                        {
                            create.Embeds.Add(BuildEmbed(update.Player, config, update));
                        }
                    }

                    create.AddAttachment("image.png", update.GetImage(), "image/png", $"{update.DisplayName} Updated {update.Entity.ShortPrefabName} @{update.Entity.transform.position} On {DateTime.Now:f}");

                    MessageComponentBuilder builder = new MessageComponentBuilder();
                    for (int i = 0; i < signMessage.Commands.Count; i++)
                    {
                        ImageMessageButtonCommand command = signMessage.Commands[i];
                        if (command.Commands.Count == 0)
                        {
                            continue;
                        }

                        if (command.Style == ButtonStyle.Link)
                        {
                            builder.AddLinkButton(command.DisplayName, ParsePlaceholders(update.Player, command.Commands[0]));
                        }
                        else
                        {
                            builder.AddActionButton(command.Style, command.DisplayName, command.CommandCustomId);
                        }
                    }

                    create.Components = builder.Build();

                    signMessage.MessageChannel?.CreateMessage(_client, create, discordMessage => { _pluginData.AddLog(discordMessage.Id, log); });
                }
            }
            finally
            {
                _log = null;
            }
        }
        
        private DiscordEmbed BuildEmbed(IPlayer player, EmbedConfig embed, BaseImageUpdate update)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            if (!string.IsNullOrEmpty(embed.Title))
            {
                builder.AddTitle(ParsePlaceholders(player, embed.Title));
            }

            if (!string.IsNullOrEmpty(embed.Description))
            {
                builder.AddDescription(ParsePlaceholders(player, embed.Description));
            }
            
            if (!string.IsNullOrEmpty(embed.Url))
            {
                builder.AddUrl(ParsePlaceholders(player, embed.Url));
            }

            if (!string.IsNullOrEmpty(embed.Image))
            {
                builder.AddImage(ParsePlaceholders(player, embed.Image));
            }
            
            if (!string.IsNullOrEmpty(embed.Thumbnail))
            {
                builder.AddThumbnail(ParsePlaceholders(player, embed.Thumbnail));
            }
            
            if (!string.IsNullOrEmpty(embed.Color))
            {
                builder.AddColor(embed.Color);
            }
            
            if (embed.Timestamp)
            {
                builder.AddNowTimestamp();
            }

            if (embed.Footer.Enabled)
            {
                if (string.IsNullOrEmpty(embed.Footer.Text) &&
                    string.IsNullOrEmpty(embed.Footer.IconUrl))
                {
                    AddPluginInfoFooter(builder);
                }
                else
                {
                    string text = ParsePlaceholders(player, embed.Footer.Text);
                    string footerUrl = ParsePlaceholders(player, embed.Footer.IconUrl);
                    builder.AddFooter(text, footerUrl);
                }
            }

            foreach (EmbedFieldConfig field in embed.Fields)
            {
                builder.AddField(ParsePlaceholders(player, field.Title), ParsePlaceholders(player, field.Value), field.Inline);
            }
            
            if (update is SignageUpdate)
            {
                SignageUpdate signage = (SignageUpdate)update;
                if (!string.IsNullOrEmpty(signage.Url))
                {
                    builder.AddField(ParsePlaceholders(player, Lang(LangKeys.SignArtistTitle)), ParsePlaceholders(player, Lang(LangKeys.SignArtistValue)), true);
                }
            }

            return builder.Build();
        }

        private const string OwnerIcon = "https://i.postimg.cc/cLGQsP1G/Sign-3.png";

        private void AddPluginInfoFooter(DiscordEmbedBuilder embed)
        {
            embed.AddFooter($"{Title} V{Version} by {Author}", OwnerIcon);
        }
    }
}