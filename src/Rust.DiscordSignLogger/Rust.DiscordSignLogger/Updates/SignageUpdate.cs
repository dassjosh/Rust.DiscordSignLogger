namespace Rust.SignLogger.Updates;

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