using System;
using Oxide.Plugins;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Enums;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=9
    public partial class DiscordSignLogger
    {
        [ConsoleCommand("dsl.erase")]
        private void EraseCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }

            uint id = arg.GetUInt(0);
            uint index = arg.GetUInt(1);
            BaseEntity entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;
            if (entity == null)
            {
                return;
            }

            if (entity is ISignage)
            {
                ISignage signage = (ISignage)entity;
                uint[] textures = signage.GetTextureCRCs();
                uint crc = textures[index];
                if (crc == 0)
                {
                    return;
                }
                FileStorage.server.RemoveExact(crc, FileStorage.Type.png, signage.NetworkID, index);
                textures[index] = 0;
                entity.SendNetworkUpdate();

                HandleReplaceImage(signage, index);
                return;
            }

            if (entity is PaintedItemStorageEntity)
            {
                PaintedItemStorageEntity item = (PaintedItemStorageEntity)entity;
                if (item._currentImageCrc != 0)
                {
                    FileStorage.server.RemoveExact(item._currentImageCrc, FileStorage.Type.png, item.net.ID, 0);
                    item._currentImageCrc = 0;
                    item.SendNetworkUpdate();
                }
            }
            
            if (entity is PatternFirework)
            {
                PatternFirework firework = (PatternFirework)entity;
                firework.Design?.Dispose();
                firework.Design = null;
                firework.SendNetworkUpdateImmediate();
            }
        }

        [ConsoleCommand("dsl.signblock")]
        private void BanCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }

            ulong playerId = arg.GetULong(0);
            float duration = arg.GetFloat(1);

            _pluginData.AddSignBan(playerId, duration);
            
            if (duration <= 0)
            {
                arg.ReplyWith($"{playerId} has been sign blocked permanently");
            }
            else
            {
                arg.ReplyWith($"{playerId} has been sign blocked for {GetFormattedDurationTime(TimeSpan.FromSeconds(duration))}");
            }
            
            SaveData();
        }

        [ConsoleCommand("dsl.signunblock")]
        private void UnbanCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                return;
            }
            
            ulong playerId = arg.GetULong(0);
            _pluginData.RemoveSignBan(playerId);
            SaveData();
            arg.ReplyWith($"{playerId} has been unbanned");
        }
        
        private void HandleReplaceImage(ISignage signage, uint index)
        {
            if (_pluginConfig.ReplaceImage.Mode == ErasedMode.None || SignArtist == null || !SignArtist.IsLoaded)
            {
                return;
            }
            
            ReplaceImageSettings image = _pluginConfig.ReplaceImage;
            if (signage is Signage)
            {
                if (image.Mode == ErasedMode.Text)
                {
                    SignArtist.Call("API_SignText", null, signage, image.Message, image.FontSize, image.TextColor, image.BodyColor, index);
                }
                else if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinSign", null, signage, image.Url, _false, index);
                }
            }
            else if (signage is PhotoFrame)
            {
                if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinPhotoFrame", null, signage, image.Url);
                }
            }
            else if (signage is CarvablePumpkin)
            {
                if (!string.IsNullOrEmpty(image.Url))
                {
                    SignArtist.Call("API_SkinPumpkin", null, signage, image.Url);
                }
            }
        }
    }
}