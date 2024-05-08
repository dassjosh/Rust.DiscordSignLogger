using Oxide.Core.Libraries.Covalence;
using Rust.SignLogger.Interfaces;

namespace Rust.SignLogger.Updates;

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