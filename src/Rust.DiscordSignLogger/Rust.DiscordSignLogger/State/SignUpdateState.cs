using System;
using System.IO;
using Oxide.Core.Libraries.Covalence;
using ProtoBuf;
using Rust.SignLogger.Ids;
using Rust.SignLogger.Interfaces;
using Rust.SignLogger.Plugins;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.State;

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