using System.Collections.Generic;
using Rust.SignLogger.Configuration;

namespace Rust.SignLogger.Updates
{
    public class PaintedItemUpdate : BaseImageUpdate
    {
        private readonly byte[] _image;

        public PaintedItemUpdate(BasePlayer player, PaintedItemStorageEntity entity, Item item, byte[] image, List<SignMessage> messages, bool ignoreMessage) : base(player, entity, messages, ignoreMessage)
        {
            _image = image;
            ItemId = item.info.itemid;
        }

        public override bool SupportsTextureIndex => false;
        public override byte[] GetImage()
        {
            return _image;
        }
    }
}