using System.Drawing;
using System.Text;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Types;
using Oxide.Plugins;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Data;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Plugins;

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
}