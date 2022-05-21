//Reference: System.Drawing
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Builders.MessageComponents;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.AllowedMentions;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Logging;
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

//DiscordSignLogger created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Discord Sign Logger", "MJSU", "1.0.6")]
    [Description("Logs Sign / Firework Changes To Discord")]
    public partial class DiscordSignLogger : RustPlugin
    {
        #region Plugins\DiscordSignLogger.Fields.cs
        [PluginReference] private Plugin RustTranslationAPI, SignArtist;
        
        [DiscordClient]
        private DiscordClient _client;
        
        private PluginConfig _pluginConfig;
        private PluginData _pluginData;
        private PluginButtonData _buttonData;
        
        public const string CommandPrefix = "DSL ";
        private const string AccentColor = "#de8732";
        
        private readonly MessageCreate _actionMessage = new MessageCreate
        {
            AllowedMention = new AllowedMention
            {
                AllowedTypes = new List<AllowedMentionTypes>(),
                Roles = new List<Snowflake>(),
                Users = new List<Snowflake>()
            }
        };
        
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly StringBuilder _actions = new StringBuilder();
        public readonly Hash<UnityEngine.Color, Brush> FireworkBrushes = new Hash<UnityEngine.Color, Brush>();
        private readonly Hash<uint, SignageUpdate> _updates = new  Hash<uint, SignageUpdate>();
        private readonly Hash<string, string> _prefabNameLookup = new Hash<string, string>();
        
        private DiscordChannel _actionChannel;
        
        private ILogEvent _log;
        private DiscordInteraction _interaction;
        private GuildMember _activeMember;
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
            
            _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            _buttonData = Interface.Oxide.DataFileSystem.ReadObject<PluginButtonData>(Name + "_Buttons");
            
            List<int> activeHash = new List<int>();
            foreach (SignMessage message in _pluginConfig.SignMessages)
            {
                foreach (ImageMessageButtonCommand command in message.Commands)
                {
                    command.SetCommandId();
                    activeHash.Add(command.CommandId);
                    _buttonData.AddOrUpdate(command);
                }
            }
            
            _buttonData.CleanupExpired(activeHash, _pluginConfig.DeleteButtonCacheAfter);
            SaveButtonData();
            
            _pluginData.CleanupExpired(_pluginConfig.DeleteLogDataAfter);
            SaveData();
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
            config.ActionLog = new ActionLogConfig(config.ActionLog);
            config.SignMessages = config.SignMessages ?? new List<SignMessage>();
            
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
            
            if (PlaceholderAPI == null || !PlaceholderAPI.IsLoaded)
            {
                PrintError("Missing plugin dependency PlaceholderAPI: https://umod.org/plugins/placeholder-api");
                return;
            }
            
            if(PlaceholderAPI.Version < new VersionNumber(2, 2, 0))
            {
                PrintError("Placeholder API plugin must be version 2.2.0 or higher");
                return;
            }
            
            if (SignArtist != null && SignArtist.IsLoaded && SignArtist.Version < new VersionNumber(1, 4, 0))
            {
                PrintWarning("Sign Artist version is outdated and may not function correctly. Please update SignArtist @ https://umod.org/plugins/sign-artist to version 1.4.0 or higher");
            }
            
            if (RustTranslationAPI == null || !RustTranslationAPI.IsLoaded)
            {
                foreach (ItemDefinition def in ItemManager.itemList)
                {
                    BaseEntity entity = def.GetComponent<ItemModDeployable>()?.entityPrefab.Get().GetComponent<BaseEntity>();
                    if (entity is ISignage || entity is PatternFirework)
                    {
                        _prefabNameLookup[entity.ShortPrefabName] = def.displayName.translated;
                    }
                }
            }
            
            _client.Connect(new DiscordSettings
            {
                Intents = GatewayIntents.Guilds,
                ApiToken = _pluginConfig.DiscordApiKey,
                LogLevel = _pluginConfig.ExtensionDebugging
            });
            
            if (SignArtist == null || !SignArtist.IsLoaded)
            {
                Unsubscribe(nameof(OnPlayerCommand));
            }
        }
        
        private void Unload()
        {
            SaveData();
            SaveButtonData();
            Instance = null;
        }
        #endregion

        #region Plugins\DiscordSignLogger.CoreHooks.cs
        private void OnImagePost(BasePlayer player, string url, bool raw, ISignage signage, uint textureIndex)
        {
            _updates[signage.NetworkID] = new SignageUpdate(player, signage, _pluginConfig.SignMessages, textureIndex, player == null, url);
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
            
            SignageUpdate update = _updates[signage.NetworkID] ?? new SignageUpdate(player, signage, _pluginConfig.SignMessages, (uint)textureIndex, player == null);
            _updates.Remove(signage.NetworkID);
            if (update.IgnoreMessage)
            {
                return;
            }
            
            SendDiscordMessage(update);
        }
        
        private void OnItemPainted(PaintedItemStorageEntity entity, Item item, BasePlayer player, byte[] image)
        {
            if (entity._currentImageCrc == 0)
            {
                return;
            }
            
            PaintedItemUpdate update = new PaintedItemUpdate(player, entity, item, image, _pluginConfig.SignMessages, false);
            SendDiscordMessage(update);
        }
        
        private void OnFireworkDesignChanged(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
        {
            if (design?.stars == null || design.stars.Count == 0)
            {
                return;
            }
            
            SendDiscordMessage(new FireworkUpdate(player, firework, _pluginConfig.SignMessages));
        }
        
        private object CanUpdateSign(BasePlayer player, BaseEntity entity)
        {
            if (!_pluginData.IsSignBanned(player))
            {
                return null;
            }
            
            Chat(player, LangKeys.BlockedMessage, GetFormattedDurationTime(_pluginData.GetRemainingBan(player)));
            
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
            
            Chat(player, LangKeys.BlockedMessage, player, GetFormattedDurationTime(_pluginData.GetRemainingBan(player), player));
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
            
            Chat(player, LangKeys.BlockedMessage, GetFormattedDurationTime(_pluginData.GetRemainingBan(player)));
            
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
        }
        
        private void SubscribeAll()
        {
            Subscribe(nameof(OnSignUpdated));
            Subscribe(nameof(OnFireworkDesignChanged));
            Subscribe(nameof(CanUpdateSign));
            Subscribe(nameof(OnFireworkDesignChange));
            
            if (SignArtist != null && SignArtist.IsLoaded)
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
                if (message.MessageChannel == null && message.ChannelId.IsValid() && guild.Channels.ContainsKey(message.ChannelId))
                {
                    message.MessageChannel = guild.Channels[message.ChannelId];
                    subscribe = true;
                }
            }
            
            if (_pluginConfig.ActionLog.ChannelId.IsValid() && guild.Channels.ContainsKey(_pluginConfig.ActionLog.ChannelId))
            {
                _actionChannel = guild.Channels[_pluginConfig.ActionLog.ChannelId];
            }
            
            if (subscribe)
            {
                SubscribeAll();
                Puts($"{Title} Ready");
            }
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordInteractionCreated)]
        private void OnDiscordInteractionCreated(DiscordInteraction interaction)
        {
            switch (interaction.Type)
            {
                case InteractionType.MessageComponent:
                HandleMessageComponentCommand(interaction);
                break;
            }
        }
        #endregion

        #region Plugins\DiscordSignLogger.DiscordInteractions.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.DiscordMethods.cs
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
        #endregion

        #region Plugins\DiscordSignLogger.Commands.cs
        [ConsoleCommand("dsl.erase")]
        private void EraseCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }
            
            uint id = arg.GetUInt(0);
            uint index = arg.GetUInt(1);
            BaseEntity entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;
            if (entity == null)
            {
                return;
            }
            
            if (entity is ISignage)
            {
                ISignage signage = (ISignage)entity;
                uint[] textures = signage.GetTextureCRCs();
                uint crc = textures[index];
                if (crc == 0)
                {
                    return;
                }
                FileStorage.server.RemoveExact(crc, FileStorage.Type.png, signage.NetworkID, index);
                textures[index] = 0;
                entity.SendNetworkUpdate();
                
                HandleReplaceImage(signage, index);
                return;
            }
            
            if (entity is PaintedItemStorageEntity)
            {
                PaintedItemStorageEntity item = (PaintedItemStorageEntity)entity;
                if (item._currentImageCrc != 0)
                {
                    FileStorage.server.RemoveExact(item._currentImageCrc, FileStorage.Type.png, item.net.ID, 0);
                    item._currentImageCrc = 0;
                    item.SendNetworkUpdate();
                }
            }
            
            if (entity is PatternFirework)
            {
                PatternFirework firework = (PatternFirework)entity;
                firework.Design?.Dispose();
                firework.Design = null;
                firework.SendNetworkUpdateImmediate();
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
            SaveData();
            
            if (duration <= 0)
            {
                arg.ReplyWith($"{playerId} has been sign blocked permanently");
            }
            else
            {
                arg.ReplyWith($"{playerId} has been sign blocked for {duration} seconds");
            }
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
            if (_pluginConfig.ReplaceImage.Mode == ErasedMode.None || SignArtist == null || !SignArtist.IsLoaded)
            {
                return;
            }
            
            ReplaceImageSettings image = _pluginConfig.ReplaceImage;
            if (signage is Signage)
            {
                if (image.Mode == ErasedMode.Text)
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
        public IPlayer FindPlayerById(string id)
        {
            return covalence.Players.FindPlayerById(id);
        }
        
        public string Lang(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player ? player.UserIDString : null);
        }
        
        public string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player ? player.UserIDString : null), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        
        public void Chat(BasePlayer player, string key) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player)));
        public void Chat(BasePlayer player, string key, params object[] args) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player, args)));
        
        public void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);
        public void SaveButtonData() => Interface.Oxide.DataFileSystem.WriteObject(Name + "_Buttons", _buttonData);
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
                [LangKeys.BlockedMessage] = "You're not allowed to update this sign/firework because you have been blocked. Your block will expire in {0}.",
                [LangKeys.ActionMessage] = $"[{Title}] <@{{dsl.discord.user.id}}> ran command \"{{dsl.command}}\"",
                [LangKeys.DeletedLog] = "The log data for this message was not found. If it's older than {0} days then it may have been deleted.",
                [LangKeys.DeletedButtonCache] = "Button was not found in cache. If this message is older than {0} days then it may have been deleted.",
                [LangKeys.SignArtistTitle] = "Sign Artist URL:",
                [LangKeys.SignArtistValue] = "{dsl.signartist.url}",
                [LangKeys.Format.Day] = "day ",
                [LangKeys.Format.Days] = "days ",
                [LangKeys.Format.Hour] = "hour ",
                [LangKeys.Format.Hours] = "hours ",
                [LangKeys.Format.Minute] = "minute ",
                [LangKeys.Format.Minutes] = "minutes ",
                [LangKeys.Format.Second] = "second",
                [LangKeys.Format.Seconds] = "seconds",
                [LangKeys.Format.TimeField] = $"<color={AccentColor}>{{0}}</color> {{1}}"
                
            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.NoPermission] = "У вас нет разрешения на выполнение этого действия",
                [LangKeys.KickReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BanReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BlockedMessage] = "Возможность использовать изображения на знаке/феерверке для вас заблокирована. Разблокировка через {0}.",
                [LangKeys.ActionMessage] = $"[{Title}] <@{{dsl.discord.user.id}}> выполнил команду \"{{dsl.command}}\"",
                [LangKeys.DeletedLog] = "Данные журнала для этого сообщения не найдены. Если сообщение старше {0} дней, возможно, оно было удалено.",
                [LangKeys.DeletedButtonCache] = "Кнопка не найдена в кеше. Если сообщение старше {0} дней, возможно, оно было удалено.",
                [LangKeys.SignArtistTitle] = "Sign Artist URL:",
                [LangKeys.SignArtistValue] = "{dsl.signartist.url}",
                [LangKeys.Format.Day] = "день ",
                [LangKeys.Format.Days] = "дней ",
                [LangKeys.Format.Hour] = "час ",
                [LangKeys.Format.Hours] = "часов ",
                [LangKeys.Format.Minute] = "минуту ",
                [LangKeys.Format.Minutes] = "минут ",
                [LangKeys.Format.Second] = "секунду",
                [LangKeys.Format.Seconds] = "секунд",
                [LangKeys.Format.TimeField] = $"<color={AccentColor}>{{0}}</color> {{1}}"
            }, this, "ru");
        }
        
        private string GetFormattedDurationTime(TimeSpan time, BasePlayer player = null)
        {
            _sb.Clear();
            
            if (time.TotalDays >= 1)
            {
                BuildTime(_sb, time.Days == 1 ? LangKeys.Format.Day : LangKeys.Format.Days, player, time.Days);
            }
            
            if (time.TotalHours >= 0)
            {
                BuildTime(_sb, time.Hours == 1 ? LangKeys.Format.Hour : LangKeys.Format.Hours, player, time.Hours);
            }
            
            if (time.TotalMinutes >= 0)
            {
                BuildTime(_sb, time.Minutes == 1 ? LangKeys.Format.Minute : LangKeys.Format.Minutes, player, time.Minutes);
            }
            
            BuildTime(_sb, time.Seconds == 1 ? LangKeys.Format.Second : LangKeys.Format.Seconds, player, time.Seconds);
            
            return _sb.ToString();
        }
        
        private void BuildTime(StringBuilder sb, string key, BasePlayer player, int value)
        {
            sb.Append(Lang(LangKeys.Format.TimeField, player, value, Lang(key, player)));
        }
        #endregion

        #region Plugins\DiscordSignLogger.PlaceholderApi.cs
        [PluginReference] private Plugin PlaceholderAPI;
        private Action<IPlayer, StringBuilder, bool> _replacer;
        
        private string ParsePlaceholders(IPlayer player, string field)
        {
            _sb.Clear();
            _sb.Append(field);
            GetReplacer()?.Invoke(player, _sb, false);
            if (_sb.Length == 0)
            {
                _sb.Append("\u200b");
            }
            return _sb.ToString();
        }
        
        private void ParsePlaceholders(IPlayer player, StringBuilder sb)
        {
            GetReplacer()?.Invoke(player, sb, false);
        }
        
        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin?.Name == "PlaceholderAPI")
            {
                _replacer = null;
            }
        }
        
        public string GetSignAristUrl()
        {
            SignageUpdate signage = _log as SignageUpdate;
            return signage?.Url ?? string.Empty;
        }
        
        private void OnPlaceholderAPIReady()
        {
            RegisterPlaceholder("dsl.entity.id", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                return entity ? entity.net?.ID ?? 0 : 0;
                
            }, "Displays the entity ID");
            
            RegisterPlaceholder("dsl.entity.textureindex", (player, s) => _log?.TextureIndex ?? 0, "Displays the texture index");
            
            RegisterPlaceholder("dsl.entity.name", (player, s) =>
            {
                if (_log.ItemId != 0)
                {
                    return GetItemName(_log.ItemId);
                }
                
                BaseEntity entity = _log.Entity;
                return entity ? GetEntityName(entity) : "Entity Not Found";
            }, "Displays the entity item name");
            
            RegisterPlaceholder("dsl.entity.owner.id", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                return entity ? entity.OwnerID : 0;
            }, "Displays the entity Owner ID");
            
            RegisterPlaceholder("dsl.entity.owner.name", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                if (!entity)
                {
                    return "Unknown";
                }
                
                IPlayer owner = covalence.Players.FindPlayerById(entity.OwnerID.ToString());
                if (owner == null)
                {
                    return "Unknown";
                }
                
                BasePlayer ownerPlayer = (BasePlayer)owner.Object;
                return ownerPlayer ? ownerPlayer.displayName : owner.Name;
            }, "Displays the entity owner player name");
            
            RegisterPlaceholder("dsl.entity.position", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                if (!entity)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        return Vector3.zero;
                    }
                    
                    return 0f;
                }
                
                Vector3 pos = entity.transform.position;
                if (string.IsNullOrEmpty(s))
                {
                    return pos;
                }
                
                if (s.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.x;
                }
                
                if (s.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.y;
                }
                
                if (s.Equals("z", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.z;
                }
                
                return pos;
            }, "Displays the position of the entity");
            
            RegisterPlaceholder("dsl.discord.user.id", (player, s) => _activeMember?.Id ?? default(Snowflake), "Discord user id who clicked the button");
            RegisterPlaceholder("dsl.discord.user.name", (player, s) => _activeMember?.DisplayName ?? "Unknown User", "Discord display name of user who clicked the button");
            RegisterPlaceholder("dsl.kick.reason", (player, s) => Lang(LangKeys.KickReason), "Kick Reason Lang Value");
            RegisterPlaceholder("dsl.ban.reason", (player, s) => Lang(LangKeys.BanReason), "Ban Reason Lang Value");
            RegisterPlaceholder("dsl.signartist.url", (player, s) => GetSignAristUrl(), "Sign Artist URL");
            RegisterPlaceholder("dsl.action.guild.id", (player, s) => _interaction?.GuildId ?? default(Snowflake), "Actioned Message Guild ID");
            RegisterPlaceholder("dsl.action.channel.id", (player, s) => _interaction?.ChannelId ?? default(Snowflake), "Actioned Message Channel ID");
            RegisterPlaceholder("dsl.action.message.id", (player, s) => _interaction.Message?.Id ?? default(Snowflake), "Actioned Message Message ID");
        }
        
        private void RegisterPlaceholder(string key, Func<IPlayer, string, object> action, string description = null)
        {
            if (IsPlaceholderApiLoaded())
            {
                PlaceholderAPI.Call("AddPlaceholder", this, key, action, description);
            }
        }
        
        private Action<IPlayer, StringBuilder, bool> GetReplacer()
        {
            if (!IsPlaceholderApiLoaded())
            {
                return _replacer;
            }
            
            return _replacer ?? (_replacer = PlaceholderAPI.Call<Action<IPlayer, StringBuilder, bool>>("GetProcessPlaceholders", 1));
        }
        
        private bool IsPlaceholderApiLoaded() => PlaceholderAPI != null && PlaceholderAPI.IsLoaded;
        #endregion

        #region Plugins\DiscordSignLogger.RustTranslationApi.cs
        public string GetEntityName(BaseEntity entity)
        {
            if (!entity)
            {
                return string.Empty;
            }
            
            if (RustTranslationAPI != null && RustTranslationAPI.IsLoaded)
            {
                string name = RustTranslationAPI.Call<string>("GetDeployableTranslation", lang.GetServerLanguage(), entity.ShortPrefabName);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            
            return _prefabNameLookup[entity.ShortPrefabName] ?? entity.ShortPrefabName;
        }
        
        public string GetItemName(int itemId)
        {
            if (itemId == 0)
            {
                return string.Empty;
            }
            
            if (RustTranslationAPI != null && RustTranslationAPI.IsLoaded)
            {
                string name = RustTranslationAPI.Call<string>("GetItemTranslationByID", lang.GetServerLanguage(), itemId);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            
            return ItemManager.FindItemDefinition(itemId).displayName.translated;
        }
        #endregion

        #region Configuration\BaseDiscordButton.cs
        public class BaseDiscordButton
        {
            [JsonProperty(PropertyName = "Button Display Name")]
            public string DisplayName { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Button Style")]
            public ButtonStyle Style { get; set; }
            
            [JsonProperty(PropertyName = "Commands")]
            public List<string> Commands { get; set; }
            
            [JsonConstructor]
            public BaseDiscordButton()
            {
                
            }
            
            public BaseDiscordButton(BaseDiscordButton settings)
            {
                DisplayName = settings?.DisplayName ?? "Button Display Name";
                Style = settings?.Style ?? ButtonStyle.Primary;
                Commands = settings?.Commands ?? new List<string>();
            }
        }
        #endregion

        #region Configuration\FireworkSettings.cs
        public class FireworkSettings
        {
            [JsonProperty(PropertyName = "Image Size (Pixels)")]
            public int ImageSize { get; set; }
            
            [JsonProperty(PropertyName = "Circle Size (Pixels)")]
            public int CircleSize { get; set; }
            
            public FireworkSettings(FireworkSettings settings)
            {
                ImageSize = settings?.ImageSize ?? 250;
                CircleSize = settings?.CircleSize ?? 19;
            }
        }
        #endregion

        #region Configuration\ImageMessageButtonCommand.cs
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
        #endregion

        #region Configuration\PluginConfig.cs
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Action Log Settings")]
            public ActionLogConfig ActionLog { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Disable Discord Button After User")]
            public bool DisableDiscordButton { get; set; }
            
            [DefaultValue(14)]
            [JsonProperty(PropertyName = "Delete Saved Log Data After (Days)")]
            public float DeleteLogDataAfter { get; set; }
            
            [DefaultValue(14)]
            [JsonProperty(PropertyName = "Delete Cached Button Data After (Days)")]
            public float DeleteButtonCacheAfter { get; set; }
            
            [JsonProperty(PropertyName = "Replace Erased Image (Requires SignArtist)")]
            public ReplaceImageSettings ReplaceImage { get; set; }
            
            [JsonProperty(PropertyName = "Firework Settings")]
            public FireworkSettings FireworkSettings { get; set; }
            
            [JsonProperty(PropertyName = "Sign Messages")]
            public List<SignMessage> SignMessages { get; set; }
            
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
            public ErasedMode Mode { get; set; }
            
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
            
            public ReplaceImageSettings(ReplaceImageSettings settings)
            {
                Mode = settings?.Mode ?? ErasedMode.Url;
                Url = settings?.Url ?? "https://i.postimg.cc/mD5xZ5R5/Erased-4.png";
                Message = settings?.Message ?? "ERASED BY ADMIN";
                FontSize = settings?.FontSize ?? 16;
                TextColor = settings?.TextColor ?? "#cd4632";
                BodyColor = settings?.BodyColor ?? "#000000";
            }
        }
        #endregion

        #region Configuration\SignMessage.cs
        public class SignMessage
        {
            [JsonProperty("Discord Channel ID")]
            public Snowflake ChannelId { get; set; }
            
            [JsonProperty(PropertyName = "Message Config")]
            public MessageConfig MessageConfig { get; set; }
            
            [JsonProperty("Button Commands")]
            public List<ImageMessageButtonCommand> Commands { get; set; }
            
            [JsonIgnore]
            public DiscordChannel MessageChannel;
            
            public SignMessage(SignMessage settings)
            {
                ChannelId = settings?.ChannelId ?? default(Snowflake);
                Commands = settings?.Commands ?? new List<ImageMessageButtonCommand>
                {
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Player Profile",
                        Style = ButtonStyle.Link,
                        Commands = new List<string> { "https://steamcommunity.com/profiles/{player.id}" },
                        PlayerMessage = string.Empty,
                        ServerMessage = string.Empty,
                        RequirePermissions = false,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Owner Profile",
                        Style = ButtonStyle.Link,
                        Commands = new List<string> { "https://steamcommunity.com/profiles/{dsl.entity.owner.id}" },
                        PlayerMessage = string.Empty,
                        ServerMessage = string.Empty,
                        RequirePermissions = false,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Erase",
                        Style = ButtonStyle.Primary,
                        Commands = new List<string> { "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                        PlayerMessage = "An admin erased your sign for being inappropriate",
                        ServerMessage = string.Empty,
                        RequirePermissions = false,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Sign Block (24 Hours)",
                        Style = ButtonStyle.Primary,
                        Commands = new List<string> { "dsl.signblock {player.id} 86400" },
                        PlayerMessage = "You have been banned from updating signs for 24 hours.",
                        ServerMessage = string.Empty,
                        RequirePermissions = true,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Kill Entity",
                        Style = ButtonStyle.Secondary,
                        Commands = new List<string> { "entid kill {dsl.entity.id}" },
                        PlayerMessage = "An admin killed your sign for being inappropriate",
                        ServerMessage = string.Empty,
                        RequirePermissions = true,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Kick Player",
                        Style = ButtonStyle.Danger,
                        Commands = new List<string> { "kick {player.id} \"{dsl.kick.reason}\"", "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                        PlayerMessage = string.Empty,
                        ServerMessage = string.Empty,
                        RequirePermissions = true,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    },
                    new ImageMessageButtonCommand
                    {
                        DisplayName = "Ban Player",
                        Style = ButtonStyle.Danger,
                        Commands = new List<string> { "ban {player.id} \"{dsl.ban.reason}\"", "dsl.erase {dsl.entity.id} {dsl.entity.textureindex}" },
                        PlayerMessage = string.Empty,
                        ServerMessage = string.Empty,
                        RequirePermissions = true,
                        AllowedRoles = new List<Snowflake>(),
                        AllowedGroups = new List<string>()
                    }
                };
                
                for (int index = 0; index < Commands.Count; index++)
                {
                    Commands[index] = new ImageMessageButtonCommand(Commands[index]);
                }
                
                MessageConfig = new MessageConfig(settings?.MessageConfig);
            }
        }
        #endregion

        #region Data\ButtonData.cs
        public class ButtonData
        {
            public ImageMessageButtonCommand Command { get; set; }
            public DateTime AddedDate { get; set; }
            
            public ButtonData(ImageMessageButtonCommand command)
            {
                Command = command;
                AddedDate = DateTime.UtcNow;
            }
        }
        #endregion

        #region Data\PluginButtonData.cs
        public class PluginButtonData
        {
            public Hash<int, ButtonData> CommandLookup = new Hash<int, ButtonData>();
            
            public void AddOrUpdate(ImageMessageButtonCommand command)
            {
                CommandLookup[command.CommandId] = new ButtonData(command);
            }
            
            public ImageMessageButtonCommand Get(int hash)
            {
                return CommandLookup[hash]?.Command;
            }
            
            public void CleanupExpired(List<int> active, float deleteAfter)
            {
                List<int> oldButtons = new List<int>();
                foreach (KeyValuePair<int, ButtonData> button in CommandLookup)
                {
                    if (active.Contains(button.Key))
                    {
                        continue;
                    }
                    
                    if ((DateTime.UtcNow - button.Value.AddedDate).TotalDays >= deleteAfter)
                    {
                        oldButtons.Add(button.Key);
                    }
                }
                
                for (int index = 0; index < oldButtons.Count; index++)
                {
                    int button = oldButtons[index];
                    CommandLookup.Remove(button);
                }
            }
        }
        #endregion

        #region Data\PluginData.cs
        public class PluginData
        {
            public Hash<Snowflake, SignUpdateLog> SignLogs = new Hash<Snowflake, SignUpdateLog>();
            public Hash<ulong, DateTime> SignBannedUsers = new Hash<ulong, DateTime>();
            
            public SignUpdateLog GetLog(Snowflake messageId)
            {
                return SignLogs[messageId];
            }
            
            public void AddLog(Snowflake messageId, SignUpdateLog data)
            {
                SignLogs[messageId] = data;
            }
            
            public void CleanupExpired(float deleteAfter)
            {
                List<Snowflake> cleanup = new List<Snowflake>();
                foreach (KeyValuePair<Snowflake, SignUpdateLog> log in SignLogs)
                {
                    if ((DateTime.UtcNow - log.Value.LogDate).TotalDays >= deleteAfter)
                    {
                        cleanup.Add(log.Key);
                    }
                }
                
                foreach (Snowflake key in cleanup)
                {
                    SignLogs.Remove(key);
                }
            }
            
            public void AddSignBan(ulong player, float duration)
            {
                SignBannedUsers[player] = duration <= 0 ? DateTime.MaxValue : DateTime.UtcNow + TimeSpan.FromMinutes(duration);
            }
            
            public void RemoveSignBan(ulong player)
            {
                SignBannedUsers.Remove(player);
            }
            
            public bool IsSignBanned(BasePlayer player)
            {
                if (!SignBannedUsers.ContainsKey(player.userID))
                {
                    return false;
                }
                
                DateTime bannedUntil = SignBannedUsers[player.userID];
                if (bannedUntil < DateTime.UtcNow)
                {
                    SignBannedUsers.Remove(player.userID);
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

        #region Data\SignUpdateLog.cs
        public class SignUpdateLog : ILogEvent
        {
            public ulong PlayerId { get; set; }
            public uint EntityId { get; set; }
            public int ItemId { get; set; }
            public uint TextureIndex { get; set; }
            
            public DateTime LogDate { get; set; }
            
            [JsonIgnore]
            private IPlayer _player;
            [JsonIgnore]
            public IPlayer Player => _player ?? (_player = DiscordSignLogger.Instance.FindPlayerById(PlayerId.ToString()));
            
            [JsonIgnore]
            private BaseEntity _entity;
            
            [JsonIgnore]
            public BaseEntity Entity
            {
                get
                {
                    if (_entity)
                    {
                        return _entity;
                    }
                    
                    if (LogDate < SaveRestore.SaveCreatedTime)
                    {
                        return null;
                    }
                    
                    _entity = BaseNetworkable.serverEntities.Find(EntityId) as BaseEntity;
                    
                    return _entity;
                }
            }
            
            [JsonConstructor]
            public SignUpdateLog()
            {
                
            }
            
            public SignUpdateLog(BaseImageUpdate update)
            {
                PlayerId = update.PlayerId;
                EntityId = update.Entity.net.ID;
                LogDate = DateTime.UtcNow;
                
                if (update.SupportsTextureIndex)
                {
                    TextureIndex = update.TextureIndex;
                }
                
                if (update is PaintedItemUpdate)
                {
                    ItemId = ((PaintedItemUpdate)update).ItemId;
                }
            }
        }
        #endregion

        #region Discord\EmbedConfig.cs
        public class EmbedConfig
        {
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Description")]
            public string Description { get; set; }
            
            [JsonProperty("Url")]
            public string Url { get; set; }
            
            [JsonProperty("Embed Color (Hex Color Code)")]
            public string Color { get; set; }
            
            [JsonProperty("Image Url")]
            public string Image { get; set; }
            
            [JsonProperty("Thumbnail Url")]
            public string Thumbnail { get; set; }
            
            [JsonProperty("Add Timestamp")]
            public bool Timestamp { get; set; }
            
            [JsonProperty(PropertyName = "Embed Fields")]
            public List<EmbedFieldConfig> Fields { get; set; }
            
            [JsonProperty("Footer")]
            public FooterConfig Footer { get; set; }
            
            public EmbedConfig(EmbedConfig settings)
            {
                Title = settings?.Title ?? "{server.name}";
                Description = settings?.Description ?? string.Empty;
                Url = settings?.Url ?? string.Empty;
                Color = settings?.Color ?? "#AC7061";
                Image = settings?.Image ?? "attachment://image.png";
                Thumbnail = settings?.Thumbnail ?? string.Empty;
                Timestamp = settings?.Timestamp ?? true;
                Fields = settings?.Fields ?? new List<EmbedFieldConfig>
                {
                    new EmbedFieldConfig
                    {
                        Title = "Player:",
                        Value = "{player.name} ([{player.id}](https://steamcommunity.com/profiles/{player.id}))",
                        Inline = true
                    },
                    new EmbedFieldConfig
                    {
                        Title = "Owner:",
                        Value = "{dsl.entity.owner.name} ([{dsl.entity.owner.id}](https://steamcommunity.com/profiles/{dsl.entity.owner.id}))",
                        Inline = true
                    },
                    new EmbedFieldConfig
                    {
                        Title = "Position:",
                        Value = "{dsl.entity.position:0.00!x} {dsl.entity.position:0.00!y} {dsl.entity.position:0.00!z}",
                        Inline = true
                    },
                    new EmbedFieldConfig
                    {
                        Title = "Item:",
                        Value = "{dsl.entity.name}",
                        Inline = true
                    },
                    new EmbedFieldConfig
                    {
                        Title = "Texture Index:",
                        Value = "{dsl.entity.textureindex}",
                        Inline = true
                    }
                };
                Footer = new FooterConfig(settings?.Footer);
            }
        }
        #endregion

        #region Discord\EmbedFieldConfig.cs
        public class EmbedFieldConfig
        {
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Value")]
            public string Value { get; set; }
            
            [JsonProperty("Inline")]
            public bool Inline { get; set; }
        }
        #endregion

        #region Discord\FooterConfig.cs
        public class FooterConfig
        {
            [JsonProperty("Icon Url")]
            public string IconUrl { get; set; }
            
            [JsonProperty("Text")]
            public string Text { get; set; }
            
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }
            
            public FooterConfig(FooterConfig settings)
            {
                IconUrl = settings?.IconUrl ?? string.Empty;
                Text = settings?.Text ?? string.Empty;
                Enabled = settings?.Enabled ?? true;
            }
        }
        #endregion

        #region Discord\MessageConfig.cs
        public class MessageConfig
        {
            [JsonProperty("content")]
            public string Content { get; set; }
            
            [JsonProperty("embeds")]
            public List<EmbedConfig> Embeds { get; set; }
            
            public MessageConfig(MessageConfig settings)
            {
                Content = settings?.Content ?? string.Empty;
                Embeds = settings?.Embeds ?? new List<EmbedConfig> { new EmbedConfig(null) };
            }
        }
        #endregion

        #region Enums\ErasedMode.cs
        public enum ErasedMode
        {
            None,
            Url,
            Text
        }
        #endregion

        #region Interfaces\ILogEvent.cs
        public interface ILogEvent
        {
            IPlayer Player { get; }
            BaseEntity Entity { get; }
            int ItemId { get; }
            uint TextureIndex { get; }
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
            public const string ActionMessage = nameof(ActionMessage);
            public const string DeletedLog = nameof(DeletedLog);
            public const string DeletedButtonCache = nameof(DeletedButtonCache);
            public const string SignArtistTitle = nameof(SignArtistTitle);
            public const string SignArtistValue = nameof(SignArtistValue);
            
            public static class Format
            {
                private const string Base = nameof(Format) + ".";
                public const string Days = Base + nameof(Days);
                public const string Hours = Base + nameof(Hours);
                public const string Minutes = Base + nameof(Minutes);
                public const string Day = Base + nameof(Day);
                public const string Hour = Base + nameof(Hour);
                public const string Minute = Base + nameof(Minute);
                public const string Second = Base + nameof(Second);
                public const string Seconds = Base + nameof(Seconds);
                public const string TimeField = Base + nameof(TimeField);
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
            public List<SignMessage> Messages { get; }
            public bool IgnoreMessage { get; }
            public int ItemId { get; protected set; }
            
            public uint TextureIndex { get; protected set; }
            public abstract bool SupportsTextureIndex { get; }
            
            protected BaseImageUpdate(BasePlayer player, BaseEntity entity, List<SignMessage> messages, bool ignoreMessage)
            {
                Player = player.IPlayer;
                DisplayName = player.displayName;
                PlayerId = player.userID;
                Entity = entity;
                Messages = messages;
                IgnoreMessage = ignoreMessage;
            }
            
            public abstract byte[] GetImage();
        }
        #endregion

        #region Updates\FireworkUpdate.cs
        public class FireworkUpdate : BaseImageUpdate
        {
            public override bool SupportsTextureIndex => false;
            public PatternFirework Firework => (PatternFirework)Entity;
            
            public FireworkUpdate(BasePlayer player, PatternFirework entity, List<SignMessage> messages) : base(player, entity, messages, false)
            {
                
            }
            
            public override byte[] GetImage()
            {
                PatternFirework firework = Firework;
                List<Star> stars = firework.Design.stars;
                
                using (Bitmap image = new Bitmap(DiscordSignLogger.Instance.FireworkImageSize, DiscordSignLogger.Instance.FireworkImageSize))
                {
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        for (int index = 0; index < stars.Count; index++)
                        {
                            Star star = stars[index];
                            int x = (int)((star.position.x + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                            int y = (int)((-star.position.y + 1) * DiscordSignLogger.Instance.FireworkHalfImageSize);
                            g.FillEllipse(GetBrush(star.color), x, y, DiscordSignLogger.Instance.FireworkCircleSize, DiscordSignLogger.Instance.FireworkCircleSize);
                        }
                        
                        return GetImageBytes(image);
                    }
                }
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
                MemoryStream stream = Pool.Get<MemoryStream>();
                image.Save(stream, ImageFormat.Png);
                byte[] bytes = stream.ToArray();
                Pool.FreeMemoryStream(ref stream);
                return bytes;
            }
        }
        #endregion

        #region Updates\PaintedItemUpdate.cs
        public class PaintedItemUpdate : BaseImageUpdate
        {
            private readonly byte[] _image;
            
            public PaintedItemUpdate(BasePlayer player, PaintedItemStorageEntity entity, Item item, byte[] image, List<SignMessage> messages, bool ignoreMessage) : base(player, entity, messages, ignoreMessage)
            {
                _image = image;
                ItemId = item.info.itemid;
            }
            
            public override bool SupportsTextureIndex => false;
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
            
            public SignageUpdate(BasePlayer player, ISignage entity, List<SignMessage> messages, uint textureIndex, bool ignoreMessage = false, string url = null) : base(player, (BaseEntity)entity, messages, ignoreMessage)
            {
                TextureIndex = textureIndex;
                Url = url;
            }
            
            public override byte[] GetImage()
            {
                ISignage sign = Signage;
                uint crc = sign.GetTextureCRCs()[TextureIndex];
                
                return FileStorage.server.Get(crc, FileStorage.Type.png, sign.NetworkID, TextureIndex);
            }
        }
        #endregion

        #region Configuration\ActionLog\ActionLogConfig.cs
        public class ActionLogConfig
        {
            [JsonProperty(PropertyName = "Channel ID")]
            public Snowflake ChannelId { get; set; }
            
            [JsonProperty(PropertyName = "Buttons")]
            public List<ActionMessageButtonCommand> Buttons { get; set; }
            
            public ActionLogConfig(ActionLogConfig settings)
            {
                ChannelId = settings?.ChannelId ?? default(Snowflake);
                Buttons = settings?.Buttons ?? new List<ActionMessageButtonCommand>
                {
                    new ActionMessageButtonCommand
                    {
                        DisplayName = "Image Message",
                        Style = ButtonStyle.Link,
                        Commands = new List<string> { "discord://-/channels/{dsl.action.guild.id}/{dsl.action.channel.id}/{dsl.action.message.id}" }
                    }
                };
                
                for (int index = 0; index < Buttons.Count; index++)
                {
                    Buttons[index] = new ActionMessageButtonCommand(Buttons[index]);
                }
            }
        }
        #endregion

        #region Configuration\ActionLog\ActionMessageButtonCommand.cs
        public class ActionMessageButtonCommand : BaseDiscordButton
        {
            [JsonConstructor]
            public ActionMessageButtonCommand()
            {
                
            }
            
            public ActionMessageButtonCommand(ActionMessageButtonCommand settings) : base(settings)
            {
                
            }
        }
        #endregion

    }

}
