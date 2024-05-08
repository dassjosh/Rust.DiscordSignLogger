using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Configuration.PluginSupport;
using Rust.SignLogger.Data;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Placeholders;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=3
public partial class DiscordSignLogger
{
    private void Init()
    {
        Instance = this;

        UnsubscribeAll();
            
        _pluginConfig.ReplaceImage.TextColor = _pluginConfig.ReplaceImage.TextColor.Replace("#", "");
        _pluginConfig.ReplaceImage.BodyColor = _pluginConfig.ReplaceImage.BodyColor.Replace("#", "");
        
        HashSet<string> ids = new();
        foreach (SignMessage message in _pluginConfig.SignMessages)
        {
            if (ids.Add(message.MessageId.Name))
            {
                _signMessages[message.MessageId] = message;
            }
            else
            {
                PrintWarning($"Duplicate Sign Message ID: '{message.MessageId.Name}'. Please check your config and correct the duplicate Sign Message ID's");
            }
        }
        
        ids.Clear();
        foreach (ImageButton button in _pluginConfig.Buttons)
        {
            if (ids.Add(button.ButtonId.Id))
            {
                _imageButtons[button.ButtonId] = button;
            }
            else
            {
                PrintWarning($"Duplicate Button ID: '{button.ButtonId.Id}'. Please check your config and correct the duplicate Image Button ID's");
            }
        }
        
        _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
        
        RegisterPlaceholders();
        RegisterTemplates();
    }
        
    protected override void LoadDefaultConfig()
    {
        PrintWarning("Loading Default Config");
    }

    protected override void LoadConfig()
    {
        base.LoadConfig();
        Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
        _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
        Config.WriteObject(_pluginConfig);
    }

    private PluginConfig AdditionalConfig(PluginConfig config)
    {
        config.FireworkSettings = new FireworkSettings(config.FireworkSettings);
        config.ReplaceImage = new ReplaceImageSettings(config.ReplaceImage);
        config.SignMessages ??= new List<SignMessage>();
        config.PluginSettings = new PluginSettings(config.PluginSettings);
        
        if (config.SignMessages.Count == 0)
        {
            config.SignMessages.Add(new SignMessage(null));
        }
        else
        {
            for (int index = 0; index < config.SignMessages.Count; index++)
            {
                config.SignMessages[index] = new SignMessage(config.SignMessages[index]);
            }
        }
        
        config.Buttons ??= new List<ImageButton>
        {
            new()
            {
                ButtonId = new ButtonId("ERASE"),
                DisplayName = "Erase",
                Style = ButtonStyle.Primary,
                Commands = new List<string> { $"dsl.erase {PlaceholderKeys.EntityId} {PlaceholderKeys.TextureIndex}" },
                PlayerMessage = "An admin erased your sign for being inappropriate",
                ServerMessage = string.Empty,
                RequirePermissions = false,
                ConfirmModal = false,
                AllowedRoles = new List<Snowflake>(),
                AllowedGroups = new List<string>()
            },
            new()
            {
                ButtonId = new ButtonId("SIGN_BLOCK_24_HOURS"),
                DisplayName = "Sign Block (24 Hours)",
                Style = ButtonStyle.Primary,
                Commands = new List<string> { "dsl.signblock {player.id} 86400" },
                PlayerMessage = "You have been banned from updating signs for 24 hours.",
                ServerMessage = string.Empty,
                RequirePermissions = true,
                ConfirmModal = false,
                AllowedRoles = new List<Snowflake>(),
                AllowedGroups = new List<string>()
            },
            new()
            {
                ButtonId = new ButtonId("KILL_ENTITY"),
                DisplayName = "Kill Entity",
                Style = ButtonStyle.Secondary,
                Commands = new List<string> { $"entid kill {PlaceholderKeys.EntityId}" },
                PlayerMessage = "An admin killed your sign for being inappropriate",
                ServerMessage = string.Empty,
                RequirePermissions = true,
                ConfirmModal = false,
                AllowedRoles = new List<Snowflake>(),
                AllowedGroups = new List<string>()
            },
            new()
            {
                ButtonId = new ButtonId("KICK_PLAYER"),
                DisplayName = "Kick Player",
                Style = ButtonStyle.Danger,
                Commands = new List<string> { 
                    $"kick {DefaultKeys.Player.Id} \"{PlaceholderKeys.PlayerMessage}\"", 
                    $"dsl.erase {PlaceholderKeys.EntityId} {PlaceholderKeys.TextureIndex}" 
                },
                PlayerMessage = string.Empty,
                ServerMessage = string.Empty,
                RequirePermissions = true,
                ConfirmModal = true,
                AllowedRoles = new List<Snowflake>(),
                AllowedGroups = new List<string>()
            },
            new()
            {
                ButtonId = new ButtonId("BAN_PLAYER"),
                DisplayName = "Ban Player",
                Style = ButtonStyle.Danger,
                Commands = new List<string>
                {
                    $"ban {DefaultKeys.Player.Id} \"{PlaceholderKeys.PlayerMessage}\"", 
                    $"dsl.erase {PlaceholderKeys.EntityId} {PlaceholderKeys.TextureIndex}"
                },
                PlayerMessage = string.Empty,
                ServerMessage = string.Empty,
                RequirePermissions = true,
                ConfirmModal = true,
                AllowedRoles = new List<Snowflake>(),
                AllowedGroups = new List<string>()
            }
        };

        for (int index = 0; index < config.Buttons.Count; index++)
        {
            config.Buttons[index] = new ImageButton(config.Buttons[index]);
        }

        return config;
    }

    private void OnServerInitialized()
    {
        FireworkCircleSize = _pluginConfig.FireworkSettings.CircleSize;
        FireworkImageSize = _pluginConfig.FireworkSettings.ImageSize + FireworkCircleSize;
        FireworkHalfImageSize = _pluginConfig.FireworkSettings.ImageSize / 2;

        if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
        {
            PrintWarning("Please set the Discord Bot Token and reload the plugin");
            return;
        }

        if (SignArtist is { IsLoaded: true })
        {
            if (SignArtist.Version < new VersionNumber(1, 4, 0))
            {
                PrintWarning("Sign Artist version is outdated and may not function correctly. Please update SignArtist @ https://umod.org/plugins/sign-artist to version 1.4.0 or higher");
            }
        }
        else
        {
            Unsubscribe(nameof(OnPlayerCommand));
        }
            
        Client.Connect(new BotConnection
        {
            Intents = GatewayIntents.Guilds,
            ApiToken = _pluginConfig.DiscordApiKey,
            LogLevel = _pluginConfig.ExtensionDebugging
        });
    }

    private void Unload()
    {
        SaveData();
        Instance = null;
    }
}