using System;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.AppCommands;
using Rust.SignLogger.Placeholders;
using Rust.SignLogger.Templates;

namespace Rust.SignLogger.Plugins;

public partial class DiscordSignLogger
{
    public void RegisterApplicationCommands()
    {
        ApplicationCommandBuilder builder = new ApplicationCommandBuilder(AppCommand.Command, "Discord Sign Logger Commands", ApplicationCommandType.ChatInput)
            .AddDefaultPermissions(PermissionFlags.None);

        AddBlockCommand(builder);
        AddUnblockCommand(builder);
        
        CommandCreate build = builder.Build();
        DiscordCommandLocalization localization = builder.BuildCommandLocalization();
            
        TemplateKey template = new("Command");
        _local.RegisterCommandLocalizationAsync(this, template, localization, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0)).Then(_ =>
        {
            _local.ApplyCommandLocalizationsAsync(this, build, template).Then(() =>
            {
                Client.Bot.Application.CreateGlobalCommand(Client, build);
            });
        });
    }

    public void AddBlockCommand(ApplicationCommandBuilder builder)
    {
        builder.AddSubCommand(AppCommand.Block, "Block a player from painting on signs", cmd =>
        {
            cmd.AddOption(CommandOptionType.String, AppArgs.Player, "Player to block",
                    options => options.Required().AutoComplete())
                .AddOption(CommandOptionType.Integer, AppArgs.Duration, "Block duration (Seconds)", options =>
                    options.Required()
                        .AddChoice("1 Hour", 60 * 60)
                        .AddChoice("12 Hours", 60 * 60 * 12)
                        .AddChoice("1 Day", 60 * 60 * 24)
                        .AddChoice("3 Days", 60 * 60 * 24 * 3)
                        .AddChoice("1 Week", 60 * 60 * 24 * 7)
                        .AddChoice("2 Weeks", 60 * 60 * 24 * 7 * 2)
                        .AddChoice("1 Month", 60 * 60 * 24 * 31)
                        .AddChoice("Forever", -1));
        });
    }

    public void AddUnblockCommand(ApplicationCommandBuilder builder)
    {
        builder.AddSubCommand(AppCommand.Unblock, "Unblock a sign blocked player", cmd =>
        {
            cmd.AddOption(CommandOptionType.String, AppArgs.Player, "Player to unblock",
                options => options.Required().AutoComplete());
        });
    }
    
    
    [DiscordAutoCompleteCommand(AppCommand.Command, AppArgs.Player, AppCommand.Block)]
    private void DiscordBlockAutoComplete(DiscordInteraction interaction, InteractionDataOption focused)
    {
        string search = focused.GetString();
        InteractionAutoCompleteBuilder response = interaction.GetAutoCompleteBuilder();
        response.AddAllOnlineFirstPlayers(search, PlayerNameFormatter.All);
        interaction.CreateResponse(Client, response);
    }

    [DiscordAutoCompleteCommand(AppCommand.Command, AppArgs.Player, AppCommand.Unblock)]
    private void DiscordUnblockAutoComplete(DiscordInteraction interaction, InteractionDataOption focused)
    {
        string search = focused.GetString();
        InteractionAutoCompleteBuilder response = interaction.GetAutoCompleteBuilder();
        response.AddPlayerList(search, GetBannedPlayers(), PlayerNameFormatter.All);
        interaction.CreateResponse(Client, response);
    }

    [DiscordApplicationCommand(AppCommand.Command, AppCommand.Block)]
    private void DiscordBlockCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
    {
        string playerId = parsed.Args.GetString(AppArgs.Player);
        IPlayer player = FindPlayerById(playerId);
        PlaceholderData data = GetPlaceholderData(interaction);
        if (player == null)
        {
            data.Add(PlaceholderDataKeys.PlayerId, playerId);
            SendTemplateResponse(interaction, TemplateKeys.Commands.Block.Errors.PlayerNotFound, data);
            return;
        }

        data.AddPlayer(player);

        if (_pluginData.IsSignBanned(playerId))
        {
            SendTemplateResponse(interaction, TemplateKeys.Commands.Block.Errors.IsAlreadyBanned, data);
            return;
        }

        int duration = parsed.Args.GetInt(AppArgs.Duration);
        _pluginData.AddSignBan(ulong.Parse(playerId), duration);
        data.AddTimeSpan(TimeSpan.FromSeconds(duration));

        SendTemplateResponse(interaction, TemplateKeys.Commands.Block.Success, data);
    }

    [DiscordApplicationCommand(AppCommand.Command, AppCommand.Unblock)]
    private void DiscordUnblockCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
    {
        string playerId = parsed.Args.GetString(AppArgs.Player);
        IPlayer player = FindPlayerById(playerId);
        PlaceholderData data = GetPlaceholderData(interaction);
        if (player == null)
        {
            data.Add(PlaceholderDataKeys.PlayerId, playerId);
            SendTemplateResponse(interaction, TemplateKeys.Commands.Unblock.Errors.PlayerNotFound, data);
            return;
        }
        
        data.AddPlayer(player);

        if (!_pluginData.IsSignBanned(playerId))
        {
            SendTemplateResponse(interaction, TemplateKeys.Commands.Unblock.Errors.NotBanned, data);
            return;
        }
        
        _pluginData.RemoveSignBan(ulong.Parse(playerId));
        SendTemplateResponse(interaction, TemplateKeys.Commands.Unblock.Success, data);
    }
}