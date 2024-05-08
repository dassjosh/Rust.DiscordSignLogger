namespace Rust.SignLogger.Updates;

public class PaintedItemUpdate : BaseImageUpdate
{
    private readonly byte[] _image;

    public PaintedItemUpdate(BasePlayer player, PaintedItemStorageEntity entity, Item item, byte[] image, bool ignoreMessage) : base(player, entity, ignoreMessage)
    {
        _image = image;
        ItemId = item.info.itemid;
    }
    
    public override byte[] GetImage()
    {
        return _image;
    }
}