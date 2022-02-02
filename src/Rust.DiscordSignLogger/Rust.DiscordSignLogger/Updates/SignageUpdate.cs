using System.Collections.Generic;
using Rust.DiscordSignLogger.Configuration;

namespace Rust.DiscordSignLogger.Updates
{
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
}