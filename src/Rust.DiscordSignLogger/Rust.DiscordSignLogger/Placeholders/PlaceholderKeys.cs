using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Plugins;

namespace Rust.SignLogger.Placeholders;

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