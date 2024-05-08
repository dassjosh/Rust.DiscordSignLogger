using Oxide.Ext.Discord.Libraries;

namespace Rust.SignLogger.Placeholders;

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