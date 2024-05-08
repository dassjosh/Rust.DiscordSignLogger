//Reference: System.Drawing
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Logging;
using Oxide.Ext.Discord.Types;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using UnityEngine;

using Color = System.Drawing.Color;
using Graphics = System.Drawing.Graphics;
using Star = ProtoBuf.PatternFirework.Star;

//DiscordSignLogger created with PluginMerge v(1.0.8.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Discord Sign Logger", "MJSU", "3.0.0")]
    [Description("Logs Sign / Firework Changes To Discord")]
    public partial class DiscordSignLogger : RustPlugin, IDiscordPlugin, IDiscordPool
    {
        #region Plugins\DiscordSignLogger.Fields.cs
        #pragma warning disable CS0649
        // ReSharper disable InconsistentNaming
        [PluginReference] private Plugin RustTranslationAPI, SignArtist;
        // ReSharper restore InconsistentNaming
        #pragma warning restore CS0649
        
        #pragma warning disable CS0649
        public DiscordClient Client { get; set; }
        #pragma warning restore CS0649
        
        private PluginConfig _pluginConfig;
        private PluginData _pluginData;
        
        private const string CommandPrefix = "DSL_CMD";
        private const string ActionPrefix = "DSL_ACTION";
        private const string ModalPrefix = "DSL_MODAL";
        private const string PlayerMessage = "PLAYER_MESSAGE";
        private const string ServerMessage = "SERVER_MESSAGE";
        private const string AccentColor = "#de8732";
        
        private readonly MessageCreate _actionMessage = new()
        {
            AllowedMentions = AllowedMentions.None
        };
        
        public DiscordPluginPool Pool { get; set; }
        
        private readonly StringBuilder _sb = new();
        public readonly Hash<UnityEngine.Color, Brush> FireworkBrushes = new();
        private readonly Hash<NetworkableId, SignageUpdate> _updates = new();
        private readonly Hash<uint, string> _prefabNameCache = new();
        private readonly Hash<int, string> _itemNameCache = new();
        private readonly Hash<TemplateKey, SignMessage> _signMessages = new();
        private readonly Hash<ButtonId, ImageButton> _imageButtons = new();
        
        private DiscordChannel _actionChannel;
        
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        private readonly DiscordMessageTemplates _templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordButtonTemplates _buttonTemplates = GetLibrary<DiscordButtonTemplates>();
        private readonly DiscordCommandLocalizations _local = GetLibrary<DiscordCommandLocalizations>();
        
        public int FireworkImageSize;
        public int FireworkHalfImageSize;
        public int FireworkCircleSize;
        
        private readonly object _true = true;
        private readonly object _false = false;
        
        public static DiscordSignLogger Instance;
        #endregion

        #region Plugins\DiscordSignLogger.Setup.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.CoreHooks.cs
        private void OnImagePost(BasePlayer player, string url, bool raw, ISignage signage, uint textureIndex)
        {
            bool ignore = player == null || !_pluginConfig.PluginSettings.SignArtist.ShouldLog(url);
            _updates[signage.NetworkID] = new SignageUpdate(player, signage, (byte)textureIndex, ignore, url);
        }
        
        private void OnSignUpdated(ISignage signage, BasePlayer player, int textureIndex = 0)
        {
            if (player == null)
            {
                _updates.Remove(signage.NetworkID);
                return;
            }
            
            if (signage.GetTextureCRCs()[textureIndex] == 0)
            {
                return;
            }
            
            SignageUpdate update = _updates[signage.NetworkID] ?? new SignageUpdate(player, signage, (byte)textureIndex, player == null);
            _updates.Remove(signage.NetworkID);
            if (update.IgnoreMessage)
            {
                return;
            }
            
            SendDiscordMessage(update);
        }
        
        private void OnItemPainted(PaintedItemStorageEntity entity, Item item, BasePlayer player, byte[] image)
        {
            if (entity._currentImageCrc != 0)
            {
                PaintedItemUpdate update = new(player, entity, item, image, false);
                SendDiscordMessage(update);
            }
        }
        
        private void OnFireworkDesignChanged(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
        {
            if (design?.stars != null && design.stars.Count != 0)
            {
                SendDiscordMessage(new FireworkUpdate(player, firework));
            }
        }
        
        private void OnCopyInfoToSign(SignContent content, ISignage sign, IUGCBrowserEntity browser)
        {
            BaseEntity entity = (BaseEntity)sign;
            BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
            SignageUpdate update = new(player, sign, 0);
            SendDiscordMessage(update);
        }
        
        private object CanUpdateSign(BasePlayer player, BaseEntity entity)
        {
            if (!_pluginData.IsSignBanned(player))
            {
                return null;
            }
            
            PlaceholderData data = GetPlaceholderData();
            data.AddTimeSpan(_pluginData.GetRemainingBan(player));
            
            Chat(player, LangKeys.BlockedMessage, data);
            
            //Client side the sign will still be updated if we block it here. We destroy the entity client side to force a redraw of the image.
            NextTick(() =>
            {
                entity.DestroyOnClient(player.Connection);
                entity.SendNetworkUpdate();
            });
            
            return _false;
        }
        
        private object OnFireworkDesignChange(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
        {
            if (!_pluginData.IsSignBanned(player))
            {
                return null;
            }
            
            PlaceholderData data = GetPlaceholderData();
            data.AddTimeSpan(_pluginData.GetRemainingBan(player));
            
            Chat(player, LangKeys.BlockedMessage, data);
            return _true;
        }
        
        private object OnPlayerCommand(BasePlayer player, string cmd, string[] args)
        {
            if (!cmd.StartsWith("sil", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            
            if (!_pluginData.IsSignBanned(player))
            {
                return null;
            }
            
            PlaceholderData data = GetPlaceholderData();
            data.AddTimeSpan(_pluginData.GetRemainingBan(player));
            
            Chat(player, LangKeys.BlockedMessage, data);
            return _true;
        }
        
        private void UnsubscribeAll()
        {
            Unsubscribe(nameof(OnImagePost));
            Unsubscribe(nameof(OnSignUpdated));
            Unsubscribe(nameof(OnFireworkDesignChanged));
            Unsubscribe(nameof(CanUpdateSign));
            Unsubscribe(nameof(OnFireworkDesignChange));
            Unsubscribe(nameof(OnPlayerCommand));
            Unsubscribe(nameof(OnCopyInfoToSign));
        }
        
        private void SubscribeAll()
        {
            Subscribe(nameof(OnSignUpdated));
            Subscribe(nameof(OnFireworkDesignChanged));
            Subscribe(nameof(CanUpdateSign));
            Subscribe(nameof(OnFireworkDesignChange));
            Subscribe(nameof(OnCopyInfoToSign));
            
            if (SignArtist is { IsLoaded: true })
            {
                Subscribe(nameof(OnPlayerCommand));
                Subscribe(nameof(OnImagePost));
            }
        }
        #endregion

        #region Plugins\DiscordSignLogger.DiscordHooks.cs
        [HookMethod(DiscordExtHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild guild)
        {
            bool subscribe = false;
            foreach (SignMessage message in _pluginConfig.SignMessages)
            {
                if (message.MessageChannel == null && message.ChannelId.IsValid())
                {
                    DiscordChannel channel = guild.GetChannel(message.ChannelId);
                    if (channel != null)
                    {
                        message.MessageChannel = channel;
                        subscribe = true;
                    }
                }
            }
            
            if (_pluginConfig.ActionLogChannel.IsValid())
            {
                DiscordChannel channel = guild.GetChannel(_pluginConfig.ActionLogChannel);
                if (channel != null)
                {
                    _actionChannel = channel;
                }
            }
            
            if (subscribe)
            {
                SubscribeAll();
                Puts($"{Title} Ready");
                RegisterApplicationCommands();
            }
        }
        #endregion

        #region Plugins\DiscordSignLogger.DiscordHelpers.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.DiscordMethods.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.Commands.cs
        [ConsoleCommand("dsl.erase")]
        private void EraseCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }
            
            NetworkableId id = arg.GetEntityID(0);
            uint index = arg.GetUInt(1);
            BaseEntity entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;
            if (!entity)
            {
                return;
            }
            
            switch (entity)
            {
                case ISignage signage:
                {
                    uint[] textures = signage.GetTextureCRCs();
                    uint crc = textures[index];
                    if (crc != 0)
                    {
                        FileStorage.server.RemoveExact(crc, FileStorage.Type.png, signage.NetworkID, index);
                        textures[index] = 0;
                        entity.SendNetworkUpdate();
                        HandleReplaceImage(signage, index);
                    }
                    
                    break;
                }
                case PaintedItemStorageEntity item:
                {
                    if (item._currentImageCrc != 0)
                    {
                        FileStorage.server.RemoveExact(item._currentImageCrc, FileStorage.Type.png, item.net.ID, 0);
                        item._currentImageCrc = 0;
                        item.SendNetworkUpdate();
                    }
                    
                    break;
                }
                case PatternFirework firework:
                firework.Design?.Dispose();
                firework.Design = null;
                firework.SendNetworkUpdateImmediate();
                break;
            }
        }
        
        [ConsoleCommand("dsl.signblock")]
        private void BanCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }
            
            ulong playerId = arg.GetULong(0);
            float duration = arg.GetFloat(1);
            
            _pluginData.AddSignBan(playerId, duration);
            
            using PlaceholderData data = GetPlaceholderData();
            data.ManualPool();
            data.AddTimeSpan(TimeSpan.FromSeconds(duration));
            
            if (duration <= 0)
            {
                arg.ReplyWith($"{playerId} has been sign blocked permanently");
            }
            else
            {
                arg.ReplyWith(_placeholders.ProcessPlaceholders($"{playerId} has been sign blocked for {DefaultKeys.Timespan.Formatted}", data));
            }
            
            SaveData();
        }
        
        [ConsoleCommand("dsl.signunblock")]
        private void UnbanCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }
            
            ulong playerId = arg.GetULong(0);
            _pluginData.RemoveSignBan(playerId);
            SaveData();
            arg.ReplyWith($"{playerId} has been unbanned");
        }
        
        private void HandleReplaceImage(ISignage signage, uint index)
        {
            if (_pluginConfig.ReplaceImage.Mode == EraseMode.None || SignArtist is not { IsLoaded: true })
            {
                return;
            }
            
            ReplaceImageSettings image = _pluginConfig.ReplaceImage;
            if (signage is Signage)
            {
                if (image.Mode == EraseMode.Text)
                {
                    SignArtist.Call("API_SignText", null, signage, image.Message, image.FontSize, image.TextColor, image.BodyColor, index);
                }
                else if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinSign", null, signage, image.Url, _false, index);
                }
            }
            else if (signage is PhotoFrame)
            {
                if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinPhotoFrame", null, signage, image.Url);
                }
            }
            else if (signage is CarvablePumpkin)
            {
                if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinPumpkin", null, signage, image.Url);
                }
            }
        }
        #endregion

        #region Plugins\DiscordSignLogger.Helpers.cs
        public IPlayer FindPlayerById(string id) => covalence.Players.FindPlayerById(id);
        
        public void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);
        
        public void Puts(string format) => base.Puts(format);
        #endregion

        #region Plugins\DiscordSignLogger.Lang.cs
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.NoPermission] = "You do not have permission to perform this action",
                [LangKeys.KickReason] = "Inappropriate sign/firework image",
                [LangKeys.BanReason] = "Inappropriate sign/firework image",
                [LangKeys.BlockedMessage] = $"You're not allowed to update this sign/firework because you have been blocked. Your block will expire in {DefaultKeys.Timespan.Formatted}.",
            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.NoPermission] = "У вас нет разрешения на выполнение этого действия",
                [LangKeys.KickReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BanReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BlockedMessage] = $"Возможность использовать изображения на знаке/феерверке для вас заблокирована. Разблокировка через {DefaultKeys.Timespan.Formatted}.",
            }, this, "ru");
        }
        
        public string Lang(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player ? player.UserIDString : null);
        }
        
        public string Lang(string key, BasePlayer player, PlaceholderData data)
        {
            return _placeholders.ProcessPlaceholders(Lang(key, player), data);
        }
        
        public string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(Lang(key, player), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        
        public void Chat(BasePlayer player, string key) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player)));
        public void Chat(BasePlayer player, string key, PlaceholderData data) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player, data)));
        #endregion

        #region Plugins\DiscordSignLogger.Placeholders.cs
        public void RegisterPlaceholders()
        {
            _placeholders.RegisterPlaceholder<SignUpdateState, ulong>(this, PlaceholderKeys.EntityId, PlaceholderDataKeys.State, state => state.EntityId);
            _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.EntityName, PlaceholderDataKeys.State, state => GetEntityName(state.Entity));
            _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.ItemName, PlaceholderDataKeys.State, state => GetItemName(state.ItemId));
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.PlayerMessage, PlaceholderDataKeys.PlayerMessage);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.ServerMessage, PlaceholderDataKeys.ServerMessage);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.SignArtistUrl, PlaceholderDataKeys.SignArtistUrl);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.Command, PlaceholderDataKeys.Command);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.ButtonId, PlaceholderDataKeys.ButtonId);
            _placeholders.RegisterPlaceholder<string>(this, PlaceholderKeys.PlayerId, PlaceholderDataKeys.PlayerId);
            _placeholders.RegisterPlaceholder<TemplateKey>(this, PlaceholderKeys.MessageId, PlaceholderDataKeys.MessageId);
            _placeholders.RegisterPlaceholder<StateKey, string>(this, PlaceholderKeys.MessageState, PlaceholderDataKeys.MessageState, state => state.State);
            _placeholders.RegisterPlaceholder<SignUpdateState, string>(this, PlaceholderKeys.TextureIndex, PlaceholderDataKeys.State, state =>
            {
                if (state.Entity is ISignage signage && signage.GetTextureCRCs().Length <= 1)
                {
                    return null;
                }
                
                return StringCache<byte>.Instance.ToString(state.TextureIndex);
            });
            _placeholders.RegisterPlaceholder<SignUpdateState, GenericPosition>(this, PlaceholderKeys.Position, PlaceholderDataKeys.State, state =>
            {
                BaseEntity entity = state.Entity;
                Vector3 pos = entity ? entity.transform.position : Vector3.zero;
                return new GenericPosition(pos.x, pos.y, pos.z);
            });
            
            PlayerPlaceholders.RegisterPlaceholders(this, PlaceholderKeys.OwnerKeys, PlaceholderDataKeys.Owner);
        }
        
        public PlaceholderData GetPlaceholderData(SignUpdateState state, DiscordInteraction interaction) => GetPlaceholderData(state).AddInteraction(interaction);
        
        public PlaceholderData GetPlaceholderData(SignUpdateState state)
        {
            return GetPlaceholderData()
            .AddPlayer(state.Player)
            .Add(PlaceholderDataKeys.State, state)
            .Add(PlaceholderDataKeys.Owner, state.Owner);
        }
        
        public PlaceholderData GetPlaceholderData(DiscordInteraction interaction) => GetPlaceholderData().AddInteraction(interaction);
        
        public PlaceholderData GetPlaceholderData()
        {
            return _placeholders.CreateData(this);
        }
        #endregion

        #region Plugins\DiscordSignLogger.Templates.cs
        public void RegisterTemplates()
        {
            HashSet<string> messages = new();
            foreach (SignMessage message in _pluginConfig.SignMessages)
            {
                if (messages.Add(message.MessageId.Name))
                {
                    _templates.RegisterGlobalTemplateAsync(this, message.MessageId, CreateDefaultTemplate(),
                    new TemplateVersion(1, 0, 1), new TemplateVersion(1, 0, 1));
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
        #endregion

        #region Plugins\DiscordSignLogger.RustTranslationApi.cs
        public string GetEntityName(BaseEntity entity)
        {
            if (!entity.IsValid())
            {
                return string.Empty;
            }
            
            if (_prefabNameCache.TryGetValue(entity.prefabID, out string name))
            {
                return name;
            }
            
            if (RustTranslationAPI is { IsLoaded: true })
            {
                name = RustTranslationAPI.Call<string>("GetDeployableTranslation", lang.GetServerLanguage(), entity.ShortPrefabName);
                if (!string.IsNullOrEmpty(name))
                {
                    _prefabNameCache[entity.prefabID] = name;
                    return name;
                }
            }
            
            _prefabNameCache[entity.prefabID] = entity.ShortPrefabName;
            return entity.ShortPrefabName;
        }
        
        public string GetItemName(int itemId)
        {
            if (itemId == 0)
            {
                return string.Empty;
            }
            
            if (_itemNameCache.TryGetValue(itemId, out string name))
            {
                return name;
            }
            
            if (RustTranslationAPI is { IsLoaded: true })
            {
                name = RustTranslationAPI.Call<string>("GetItemTranslationByID", lang.GetServerLanguage(), itemId);
                if (!string.IsNullOrEmpty(name))
                {
                    _itemNameCache[itemId] = name;
                    return name;
                }
            }
            
            name = ItemManager.FindItemDefinition(itemId).displayName.translated;
            _itemNameCache[itemId] = name;
            return name;
        }
        #endregion

        #region Plugins\DiscordSignLogger.AppCommands.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.DiscordInteractions.cs
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
        #endregion

        #region AppCommands\AppArgs.cs
        public class AppArgs
        {
            public const string Player = "player";
            public const string Duration = "duration";
        }
        #endregion

        #region AppCommands\AppCommand.cs
        public class AppCommand
        {
            public const string Command = "dsl";
            public const string Block = "block";
            public const string Unblock = "unblock";
        }
        #endregion

        #region Configuration\FireworkSettings.cs
        public class FireworkSettings
        {
            [JsonProperty(PropertyName = "Image Size (Pixels)")]
            public int ImageSize { get; set; }
            
            [JsonProperty(PropertyName = "Circle Size (Pixels)")]
            public int CircleSize { get; set; }
            
            [JsonConstructor]
            private FireworkSettings() { }
            
            public FireworkSettings(FireworkSettings settings)
            {
                ImageSize = settings?.ImageSize ?? 250;
                CircleSize = settings?.CircleSize ?? 19;
            }
        }
        #endregion

        #region Configuration\ImageButton.cs
        public class ImageButton
        {
            [JsonProperty(PropertyName = "Button ID")]
            public ButtonId ButtonId { get; set; }
            
            [JsonProperty(PropertyName = "Button Display Name")]
            public string DisplayName { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Button Style")]
            public ButtonStyle Style { get; set; }
            
            [JsonProperty(PropertyName = "Commands")]
            public List<string> Commands { get; set; }
            
            [JsonProperty(PropertyName = "Player Message")]
            public string PlayerMessage { get; set; }
            
            [JsonProperty(PropertyName = "Server Message")]
            public string ServerMessage { get; set; }
            
            [JsonProperty(PropertyName = "Show Confirmation Modal")]
            public bool ConfirmModal { get; set; }
            
            [JsonProperty(PropertyName = "Requires Permissions To Use Button")]
            public bool RequirePermissions { get; set; }
            
            [JsonProperty(PropertyName = "Allowed Discord Roles (Role ID)")]
            public List<Snowflake> AllowedRoles { get; set; }
            
            [JsonProperty(PropertyName = "Allowed Oxide Groups (Group Name)")]
            public List<string> AllowedGroups { get; set; }
            
            [JsonConstructor]
            public ImageButton() { }
            
            public ImageButton(ImageButton settings)
            {
                ButtonId = settings.ButtonId;
                DisplayName = settings.DisplayName ?? "Button Display Name";
                Style = settings.Style;
                Commands = settings.Commands ?? new List<string>();
                PlayerMessage = settings.PlayerMessage ?? string.Empty;
                ServerMessage = settings.ServerMessage ?? string.Empty;
                ConfirmModal = settings.ConfirmModal;
                RequirePermissions = settings.RequirePermissions;
                AllowedRoles = settings.AllowedRoles ?? new List<Snowflake>();
                AllowedGroups = settings.AllowedGroups ?? new List<string>();
            }
        }
        #endregion

        #region Configuration\PluginConfig.cs
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Disable Discord Button After Use")]
            public bool DisableDiscordButton { get; set; }
            
            [JsonProperty(PropertyName = "Action Log Channel ID")]
            public Snowflake ActionLogChannel { get; set; }
            
            [JsonProperty(PropertyName = "Replace Erased Image (Requires SignArtist)")]
            public ReplaceImageSettings ReplaceImage { get; set; }
            
            [JsonProperty(PropertyName = "Firework Settings")]
            public FireworkSettings FireworkSettings { get; set; }
            
            [JsonProperty(PropertyName = "Sign Messages")]
            public List<SignMessage> SignMessages { get; set; }
            
            [JsonProperty(PropertyName = "Buttons")]
            public List<ImageButton> Buttons { get; set; }
            
            public PluginSettings PluginSettings { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; } = DiscordLogLevel.Info;
        }
        #endregion

        #region Configuration\ReplaceImageSettings.cs
        public class ReplaceImageSettings
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Replaced Mode (None, Url, Text)")]
            public EraseMode Mode { get; set; }
            
            [JsonProperty(PropertyName = "URL")]
            public string Url { get; set; }
            
            [JsonProperty(PropertyName = "Message")]
            public string Message { get; set; }
            
            [JsonProperty(PropertyName = "Font Size")]
            public int FontSize { get; set; }
            
            [JsonProperty(PropertyName = "Text Color")]
            public string TextColor { get; set; }
            
            [JsonProperty(PropertyName = "Body Color")]
            public string BodyColor { get; set; }
            
            [JsonConstructor]
            private ReplaceImageSettings() { }
            
            public ReplaceImageSettings(ReplaceImageSettings settings)
            {
                Mode = settings?.Mode ?? EraseMode.Url;
                Url = settings?.Url ?? "https://i.postimg.cc/mD5xZ5R5/Erased-4.png";
                Message = settings?.Message ?? "ERASED BY ADMIN";
                FontSize = settings?.FontSize ?? 16;
                TextColor = settings?.TextColor ?? "#cd4632";
                BodyColor = settings?.BodyColor ?? "#000000";
            }
            
            private bool ShouldSerializeUrl() => Mode == EraseMode.Url;
            private bool ShouldSerializeMessage() => Mode == EraseMode.Text;
            private bool ShouldSerializeFontSize() => Mode == EraseMode.Text;
            private bool ShouldSerializeTextColor() => Mode == EraseMode.Text;
            private bool ShouldSerializeBodyColor() => Mode == EraseMode.Text;
        }
        #endregion

        #region Configuration\SignMessage.cs
        public class SignMessage
        {
            [JsonProperty("Message ID")]
            public TemplateKey MessageId { get; set; }
            
            [JsonProperty("Discord Channel ID")]
            public Snowflake ChannelId { get; set; }
            
            [JsonProperty("Use Action Button")]
            public bool UseActionButton { get; set; } = true;
            
            [JsonProperty("Buttons")]
            public List<ButtonId> Buttons { get; set; }
            
            [JsonIgnore]
            public DiscordChannel MessageChannel;
            
            [JsonConstructor]
            private SignMessage() { }
            
            public SignMessage(SignMessage settings)
            {
                MessageId = settings?.MessageId ?? new TemplateKey("DEFAULT");
                if (!MessageId.IsValid)
                {
                    MessageId = new TemplateKey("DEFAULT");
                }
                ChannelId = settings?.ChannelId ?? default(Snowflake);
                UseActionButton = settings?.UseActionButton ?? true;
                Buttons = settings?.Buttons ?? new List<ButtonId>
                {
                    new("ERASE"),
                    new("SIGN_BLOCK_24_HOURS"),
                    new("KILL_ENTITY"),
                    new("KICK_PLAYER"),
                    new("BAN_PLAYER"),
                };
            }
        }
        #endregion

        #region Data\PluginData.cs
        public class PluginData
        {
            public Hash<ulong, DateTime> SignBannedUsers = new();
            
            public void AddSignBan(ulong player, float duration)
            {
                SignBannedUsers[player] = duration <= 0 ? DateTime.MaxValue : DateTime.UtcNow + TimeSpan.FromSeconds(duration);
            }
            
            public void RemoveSignBan(ulong player)
            {
                SignBannedUsers.Remove(player);
            }
            
            public bool IsSignBanned(BasePlayer player) => IsSignBanned(player.userID);
            public bool IsSignBanned(string playerId) => IsSignBanned(ulong.Parse(playerId));
            
            public bool IsSignBanned(ulong playerId)
            {
                if (!SignBannedUsers.ContainsKey(playerId))
                {
                    return false;
                }
                
                DateTime bannedUntil = SignBannedUsers[playerId];
                if (bannedUntil < DateTime.UtcNow)
                {
                    SignBannedUsers.Remove(playerId);
                    return false;
                }
                
                return true;
            }
            
            public TimeSpan GetRemainingBan(BasePlayer player)
            {
                return SignBannedUsers[player.userID] - DateTime.UtcNow;
            }
        }
        #endregion

        #region Enums\EraseMode.cs
        public enum EraseMode
        {
            None,
            Url,
            Text
        }
        #endregion

        #region Ids\ButtonId.cs
        [JsonConverter(typeof(ButtonIdConverter))]
        public readonly struct ButtonId : IEquatable<ButtonId>
        {
            public readonly string Id;
            
            public ButtonId(string id)
            {
                Id = id;
            }
            
            public bool Equals(ButtonId other) => Id == other.Id;
            
            public override bool Equals(object obj) => obj is ButtonId other && Equals(other);
            
            public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;
        }
        #endregion

        #region Ids\StateKey.cs
        public readonly struct StateKey : IDiscordKey
        {
            public readonly string State;
            
            public StateKey(string state)
            {
                State = state;
            }
            
            public override string ToString() => State;
        }
        #endregion

        #region Interfaces\ILogEvent.cs
        public interface ILogEvent
        {
            IPlayer Player { get; }
            BaseEntity Entity { get; }
            int ItemId { get; }
            byte TextureIndex { get; }
        }
        #endregion

        #region Json\ButtonIdConverter.cs
        public class ButtonIdConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                ButtonId id = (ButtonId)value;
                writer.WriteValue(id.Id);
            }
            
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    return new ButtonId(reader.Value.ToString());
                }
                
                throw new JsonException($"Unexpected token {reader.TokenType} when parsing ButtonID.");
            }
            
            public override bool CanConvert(Type objectType)
            {
                return typeof(ButtonId) == objectType;
            }
        }
        #endregion

        #region Lang\LangKeys.cs
        public static class LangKeys
        {
            public const string Chat = nameof(Chat);
            public const string NoPermission = nameof(NoPermission);
            public const string KickReason = nameof(KickReason);
            public const string BanReason = nameof(BanReason);
            public const string BlockedMessage = nameof(BlockedMessage);
        }
        #endregion

        #region Placeholders\PlaceholderDataKeys.cs
        public class PlaceholderDataKeys
        {
            public static readonly PlaceholderDataKey Owner = new("owner");
            public static readonly PlaceholderDataKey State = new("state");
            public static readonly PlaceholderDataKey PlayerMessage = new("message.player");
            public static readonly PlaceholderDataKey ServerMessage = new("message.server");
            public static readonly PlaceholderDataKey SignArtistUrl = new("signartist.url");
            public static readonly PlaceholderDataKey Command = new("command");
            public static readonly PlaceholderDataKey ButtonId = new("buttonid");
            public static readonly PlaceholderDataKey MessageId = new("message.id");
            public static readonly PlaceholderDataKey MessageState = new("message.staate");
            public static readonly PlaceholderDataKey PlayerId = new("playerid");
        }
        #endregion

        #region Placeholders\PlaceholderKeys.cs
        public class PlaceholderKeys
        {
            public static readonly PlaceholderKey EntityId = new(nameof(DiscordSignLogger), "entity.id");
            public static readonly PlaceholderKey EntityName = new(nameof(DiscordSignLogger), "entity.name");
            public static readonly PlaceholderKey TextureIndex = new(nameof(DiscordSignLogger), "entity.textureindex");
            public static readonly PlaceholderKey Position = new(nameof(DiscordSignLogger), "entity.position");
            public static readonly PlaceholderKey ItemName = new(nameof(DiscordSignLogger), "item.name");
            public static readonly PlaceholderKey PlayerMessage = new(nameof(DiscordSignLogger), "message.player");
            public static readonly PlaceholderKey ServerMessage = new(nameof(DiscordSignLogger), "message.server");
            public static readonly PlaceholderKey SignArtistUrl = new(nameof(DiscordSignLogger), "signartist.url");
            public static readonly PlaceholderKey Command = new(nameof(DiscordSignLogger), "command");
            public static readonly PlaceholderKey ButtonId = new(nameof(DiscordSignLogger), "error.buttonid");
            public static readonly PlaceholderKey MessageId = new(nameof(DiscordSignLogger), "message.id");
            public static readonly PlaceholderKey MessageState = new(nameof(DiscordSignLogger), "message.state");
            public static readonly PlaceholderKey PlayerId = new(nameof(DiscordSignLogger), "player.id");
            
            public static readonly PlayerKeys OwnerKeys = new($"{nameof(DiscordSignLogger)}.owner");
            
        }
        #endregion

        #region State\SignUpdateState.cs
        [ProtoContract]
        public class SignUpdateState
        {
            [ProtoMember(1)]
            public ulong PlayerId { get; set; }
            
            [ProtoMember(2)]
            public ulong EntityId { get; set; }
            
            [ProtoMember(3)]
            public byte TextureIndex { get; set; }
            
            [ProtoMember(4)]
            public int ItemId { get; set; }
            
            private IPlayer _player;
            public IPlayer Player => _player ??= DiscordSignLogger.Instance.FindPlayerById(PlayerId.ToString());
            
            private IPlayer _owner;
            public IPlayer Owner => _owner ??= DiscordSignLogger.Instance.FindPlayerById(Entity.IsValid() ? Entity.OwnerID.ToString() : "0");
            
            private BaseEntity _entity;
            public BaseEntity Entity => _entity ??= BaseNetworkable.serverEntities.Find(new NetworkableId(EntityId)) as BaseEntity;
            
            public SignUpdateState() { }
            
            public SignUpdateState(BaseImageUpdate update)
            {
                PlayerId = update.PlayerId;
                EntityId = update.Entity.net.ID.Value;
                
                if (update.SupportsTextureIndex)
                {
                    TextureIndex = update.TextureIndex;
                }
                
                if (update is PaintedItemUpdate itemUpdate)
                {
                    ItemId = itemUpdate.ItemId;
                }
            }
            
            public StateKey Serialize()
            {
                MemoryStream stream = DiscordSignLogger.Instance.Pool.GetMemoryStream();
                Serializer.Serialize(stream, this);
                stream.TryGetBuffer(out ArraySegment<byte> buffer);
                string base64 = Convert.ToBase64String(buffer.AsSpan());
                DiscordSignLogger.Instance.Pool.FreeMemoryStream(stream);
                return new StateKey(base64);
            }
            
            public static SignUpdateState Deserialize(ReadOnlySpan<char> base64)
            {
                Span<byte> buffer = stackalloc byte[64];
                Convert.TryFromBase64Chars(base64, buffer, out int written);
                MemoryStream stream = DiscordSignLogger.Instance.Pool.GetMemoryStream();
                stream.Write(buffer[..written]);
                stream.Flush();
                stream.Position = 0;
                SignUpdateState state = Serializer.Deserialize<SignUpdateState>(stream);
                DiscordSignLogger.Instance.Pool.FreeMemoryStream(stream);
                return state;
            }
        }
        #endregion

        #region Templates\TemplateKeys.cs
        public class TemplateKeys
        {
            public static readonly TemplateKey NoPermission = new(nameof(NoPermission));
            
            public static class Action
            {
                private const string Base = nameof(Action) + ".";
                
                public static readonly TemplateKey Message = new(Base + nameof(Message));
                public static readonly TemplateKey Button = new(Base + nameof(Button));
            }
            
            public static class Errors
            {
                private const string Base = nameof(Errors) + ".";
                
                public static readonly TemplateKey FailedToParse = new(Base + nameof(FailedToParse));
                public static readonly TemplateKey ButtonIdNotFound = new(Base + nameof(ButtonIdNotFound));
            }
            
            public static class Commands
            {
                private const string Base = nameof(Commands) + ".";
                
                public static class Block
                {
                    private const string Base = Commands.Base + nameof(Block) + ".";
                    
                    public static readonly TemplateKey Success = new(Base + nameof(Success));
                    
                    public static class Errors
                    {
                        private const string Base = Block.Base + nameof(Errors) + ".";
                        
                        public static readonly TemplateKey PlayerNotFound = new(Base + nameof(PlayerNotFound));
                        public static readonly TemplateKey IsAlreadyBanned = new(Base + nameof(IsAlreadyBanned));
                    }
                }
                
                public static class Unblock
                {
                    private const string Base = Commands.Base + nameof(Unblock) + ".";
                    
                    public static readonly TemplateKey Success = new(Base + nameof(Success));
                    
                    public static class Errors
                    {
                        private const string Base = Unblock.Base + nameof(Errors) + ".";
                        
                        public static readonly TemplateKey PlayerNotFound = new(Base + nameof(PlayerNotFound));
                        public static readonly TemplateKey NotBanned = new(Base + nameof(NotBanned));
                    }
                }
            }
        }
        #endregion

        #region Updates\BaseImageUpdate.cs
        public abstract class BaseImageUpdate : ILogEvent
        {
            public IPlayer Player { get; }
            public ulong PlayerId { get; }
            public string DisplayName { get; }
            public BaseEntity Entity { get; }
            public bool IgnoreMessage { get; }
            public int ItemId { get; protected set; }
            
            public byte TextureIndex { get; protected set; }
            public virtual bool SupportsTextureIndex => false;
            
            protected BaseImageUpdate(BasePlayer player, BaseEntity entity, bool ignoreMessage)
            {
                Player = player.IPlayer;
                DisplayName = player.displayName;
                PlayerId = player.userID;
                Entity = entity;
                IgnoreMessage = ignoreMessage;
            }
            
            public abstract byte[] GetImage();
        }
        #endregion

        #region Updates\FireworkUpdate.cs
        public class FireworkUpdate : BaseImageUpdate
        {
            public PatternFirework Firework => (PatternFirework)Entity;
            
            public FireworkUpdate(BasePlayer player, PatternFirework entity) : base(player, entity, false) { }
            
            public override byte[] GetImage()
            {
                PatternFirework firework = Firework;
                List<Star> stars = firework.Design.stars;
                
                using Bitmap image = new(DiscordSignLogger.Instance.FireworkImageSize, DiscordSignLogger.Instance.FireworkImageSize);
                using Graphics g = Graphics.FromImage(image);
                for (int index = 0; index < stars.Count; index++)
                {
                    Star star = stars[index];
                    int x = (int)((star.position.x + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                    int y = (int)((-star.position.y + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                    g.FillEllipse(GetBrush(star.color), x, y, DiscordSignLogger.Instance.FireworkCircleSize, DiscordSignLogger.Instance.FireworkCircleSize);
                }
                
                return GetImageBytes(image);
            }
            
            private Brush GetBrush(UnityEngine.Color color)
            {
                Brush brush = DiscordSignLogger.Instance.FireworkBrushes[color];
                if (brush == null)
                {
                    brush = new SolidBrush(FromUnityColor(color));
                    DiscordSignLogger.Instance.FireworkBrushes[color] = brush;
                }
                
                return brush;
            }
            
            private Color FromUnityColor(UnityEngine.Color color)
            {
                int red = FromUnityColorField(color.r);
                int green = FromUnityColorField(color.g);
                int blue = FromUnityColorField(color.b);
                int alpha = FromUnityColorField(color.a);
                
                return Color.FromArgb(alpha, red, green, blue);
            }
            
            private int FromUnityColorField(float color)
            {
                return (int)(color * 255);
            }
            
            private byte[] GetImageBytes(Bitmap image)
            {
                MemoryStream stream = DiscordSignLogger.Instance.Pool.GetMemoryStream();
                image.Save(stream, ImageFormat.Png);
                byte[] bytes = stream.ToArray();
                DiscordSignLogger.Instance.Pool.FreeMemoryStream(stream);
                return bytes;
            }
        }
        #endregion

        #region Updates\PaintedItemUpdate.cs
        public class PaintedItemUpdate : BaseImageUpdate
        {
            private readonly byte[] _image;
            
            public PaintedItemUpdate(BasePlayer player, PaintedItemStorageEntity entity, Item item, byte[] image, bool ignoreMessage) : base(player, entity, ignoreMessage)
            {
                _image = image;
                ItemId = item.info.itemid;
            }
            
            public override byte[] GetImage()
            {
                return _image;
            }
        }
        #endregion

        #region Updates\SignageUpdate.cs
        public class SignageUpdate : BaseImageUpdate
        {
            public string Url { get; }
            public override bool SupportsTextureIndex => true;
            public ISignage Signage => (ISignage)Entity;
            
            public SignageUpdate(BasePlayer player, ISignage entity, byte textureIndex, bool ignoreMessage = false, string url = null) : base(player, (BaseEntity)entity, ignoreMessage)
            {
                TextureIndex = textureIndex;
                Url = url;
            }
            
            public override byte[] GetImage()
            {
                ISignage sign = Signage;
                uint crc = sign.GetTextureCRCs()[TextureIndex];
                return FileStorage.server.Get(crc, FileStorage.Type.png, sign.NetworkID, (uint)TextureIndex);
            }
        }
        #endregion

        #region Configuration\PluginSupport\PluginSettings.cs
        public class PluginSettings
        {
            [JsonProperty("Sign Artist Settings")]
            public SignArtistSettings SignArtist { get; set; }
            
            [JsonConstructor]
            private PluginSettings() { }
            
            public PluginSettings(PluginSettings settings)
            {
                SignArtist = new SignArtistSettings(settings?.SignArtist);
            }
        }
        #endregion

        #region Configuration\PluginSupport\SignArtistSettings.cs
        public class SignArtistSettings
        {
            [JsonProperty("Log /sil")]
            public bool LogSil { get; set; }
            
            [JsonProperty("Log /sili")]
            public bool LogSili { get; set; }
            
            [JsonProperty("Log /silt")]
            public bool LogSilt { get; set; }
            
            [JsonConstructor]
            private SignArtistSettings() { }
            
            public SignArtistSettings(SignArtistSettings settings)
            {
                LogSil = settings?.LogSil ?? true;
                LogSili = settings?.LogSili ?? true;
                LogSilt = settings?.LogSilt ?? true;
            }
            
            public bool ShouldLog(string url)
            {
                if (url.StartsWith("http://assets.imgix.net"))
                {
                    return LogSilt;
                }
                
                if (ItemManager.itemDictionaryByName.ContainsKey(url))
                {
                    return LogSili;
                }
                
                return LogSil;
            }
        }
        #endregion

    }

}
