using System;
using Oxide.Ext.Discord.Libraries;
using Oxide.Plugins;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Enums;
using UnityEngine;

namespace Rust.SignLogger.Plugins;

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

        NetworkableId id = arg.GetEntityID(0);
        uint index = arg.GetUInt(1);
        BaseEntity entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;
        if (!entity)
        {
            return;
        }

        switch (entity)
        {
            case ISignage signage:
            {
                uint[] textures = signage.GetTextureCRCs();
                uint crc = textures[index];
                if (crc != 0)
                {
                    FileStorage.server.RemoveExact(crc, FileStorage.Type.png, signage.NetworkID, index);
                    textures[index] = 0;
                    entity.SendNetworkUpdate();
                    HandleReplaceImage(signage, index);
                }

                break;
            }
            case PaintedItemStorageEntity item:
            {
                if (item._currentImageCrc != 0)
                {
                    FileStorage.server.RemoveExact(item._currentImageCrc, FileStorage.Type.png, item.net.ID, 0);
                    item._currentImageCrc = 0;
                    item.SendNetworkUpdate();
                }

                break;
            }
            case PatternFirework firework:
                firework.Design?.Dispose();
                firework.Design = null;
                firework.SendNetworkUpdateImmediate();
                break;
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

        using PlaceholderData data = GetPlaceholderData();
        data.ManualPool();
        data.AddTimeSpan(TimeSpan.FromSeconds(duration));
            
        if (duration <= 0)
        {
            arg.ReplyWith($"{playerId} has been sign blocked permanently");
        }
        else
        {
            arg.ReplyWith(_placeholders.ProcessPlaceholders($"{playerId} has been sign blocked for {DefaultKeys.Timespan.Formatted}", data));
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
        if (_pluginConfig.ReplaceImage.Mode == EraseMode.None || SignArtist is not { IsLoaded: true })
        {
            return;
        }
            
        ReplaceImageSettings image = _pluginConfig.ReplaceImage;
        if (signage is Signage)
        {
            if (image.Mode == EraseMode.Text)
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