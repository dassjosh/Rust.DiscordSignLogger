using System;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Lang;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=4
public partial class DiscordSignLogger
{
    private void OnImagePost(BasePlayer player, string url, bool raw, ISignage signage, uint textureIndex)
    {
        bool ignore = player == null || !_pluginConfig.PluginSettings.SignArtist.ShouldLog(url);
        _updates[signage.NetworkID] = new SignageUpdate(player, signage, (byte)textureIndex, ignore, url);
    }

    private void OnSignUpdated(ISignage signage, BasePlayer player, int textureIndex = 0)
    {
        if (player == null)
        {
            _updates.Remove(signage.NetworkID);
            return;
        }
            
        if (signage.GetTextureCRCs()[textureIndex] == 0)
        {
            return;
        }

        SignageUpdate update = _updates[signage.NetworkID] ?? new SignageUpdate(player, signage, (byte)textureIndex, player == null);
        _updates.Remove(signage.NetworkID);
        if (update.IgnoreMessage)
        {
            return;
        }
        
        SendDiscordMessage(update);
    }

    private void OnItemPainted(PaintedItemStorageEntity entity, Item item, BasePlayer player, byte[] image)
    {
        if (entity._currentImageCrc != 0)
        {
            PaintedItemUpdate update = new(player, entity, item, image, false);
            SendDiscordMessage(update);
        }
    }

    private void OnFireworkDesignChanged(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
    {
        if (design?.stars != null && design.stars.Count != 0)
        {
            SendDiscordMessage(new FireworkUpdate(player, firework));
        }
    }
    
    private void OnCopyInfoToSign(SignContent content, ISignage sign, IUGCBrowserEntity browser)
    {
        BaseEntity entity = (BaseEntity)sign;
        BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
        SignageUpdate update = new(player, sign, 0);
        SendDiscordMessage(update);
    }

    private object CanUpdateSign(BasePlayer player, BaseEntity entity)
    {
        if (!_pluginData.IsSignBanned(player))
        {
            return null;
        }
        
        PlaceholderData data = GetPlaceholderData();
        data.AddTimeSpan(_pluginData.GetRemainingBan(player));

        Chat(player, LangKeys.BlockedMessage, data);
            
        //Client side the sign will still be updated if we block it here. We destroy the entity client side to force a redraw of the image.
        NextTick(() =>
        {
            entity.DestroyOnClient(player.Connection);
            entity.SendNetworkUpdate();
        });

        return _false;
    }

    private object OnFireworkDesignChange(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
    {
        if (!_pluginData.IsSignBanned(player))
        {
            return null;
        }
            
        PlaceholderData data = GetPlaceholderData();
        data.AddTimeSpan(_pluginData.GetRemainingBan(player));
        
        Chat(player, LangKeys.BlockedMessage, data);
        return _true;
    }

    private object OnPlayerCommand(BasePlayer player, string cmd, string[] args)
    {
        if (!cmd.StartsWith("sil", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!_pluginData.IsSignBanned(player))
        {
            return null;
        }
        
        PlaceholderData data = GetPlaceholderData();
        data.AddTimeSpan(_pluginData.GetRemainingBan(player));
            
        Chat(player, LangKeys.BlockedMessage, data);
        return _true;
    }

    private void UnsubscribeAll()
    {
        Unsubscribe(nameof(OnImagePost));
        Unsubscribe(nameof(OnSignUpdated));
        Unsubscribe(nameof(OnFireworkDesignChanged));
        Unsubscribe(nameof(CanUpdateSign));
        Unsubscribe(nameof(OnFireworkDesignChange));
        Unsubscribe(nameof(OnPlayerCommand));
        Unsubscribe(nameof(OnCopyInfoToSign));
    }

    private void SubscribeAll()
    {
        Subscribe(nameof(OnSignUpdated));
        Subscribe(nameof(OnFireworkDesignChanged));
        Subscribe(nameof(CanUpdateSign));
        Subscribe(nameof(OnFireworkDesignChange));
        Subscribe(nameof(OnCopyInfoToSign));
            
        if (SignArtist is { IsLoaded: true })
        {
            Subscribe(nameof(OnPlayerCommand));
            Subscribe(nameof(OnImagePost));
        }
    }
}