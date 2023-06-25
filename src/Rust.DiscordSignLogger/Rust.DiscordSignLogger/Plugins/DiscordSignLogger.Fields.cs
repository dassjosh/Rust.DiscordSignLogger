using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.AllowedMentions;
using Oxide.Plugins;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Data;
using Rust.SignLogger.Interfaces;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=2
    public partial class DiscordSignLogger
    {
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
        private readonly Hash<NetworkableId, SignageUpdate> _updates = new  Hash<NetworkableId, SignageUpdate>();
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