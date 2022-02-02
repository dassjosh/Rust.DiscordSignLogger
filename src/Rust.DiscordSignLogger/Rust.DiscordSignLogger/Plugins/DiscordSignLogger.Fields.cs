using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.AllowedMentions;
using Oxide.Plugins;
using Rust.DiscordSignLogger.Configuration;
using Rust.DiscordSignLogger.Data;
using Rust.DiscordSignLogger.Interfaces;
using Rust.DiscordSignLogger.Updates;

namespace Rust.DiscordSignLogger.Plugins
{
    //Define:FileOrder=2
    public partial class DiscordSignLogger
    {
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
    }
}