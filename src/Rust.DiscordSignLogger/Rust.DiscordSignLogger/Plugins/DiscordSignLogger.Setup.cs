using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Entities.Gatway;
using Rust.DiscordSignLogger.Configuration;
using Rust.DiscordSignLogger.Configuration.ActionLog;
using Rust.DiscordSignLogger.Data;

namespace Rust.DiscordSignLogger.Plugins
{
    //Define:FileOrder=3
    public partial class DiscordSignLogger
    {
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
    }
}