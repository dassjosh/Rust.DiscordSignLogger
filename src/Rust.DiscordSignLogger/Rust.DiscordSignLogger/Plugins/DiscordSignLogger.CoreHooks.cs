using System;
using Rust.SignLogger.Lang;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=4
    public partial class DiscordSignLogger
    {
        private void OnImagePost(BasePlayer player, string url, bool raw, ISignage signage, uint textureIndex)
        {
            _updates[signage.NetworkID] = new SignageUpdate(player, signage, _pluginConfig.SignMessages, textureIndex, player == null, url);
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

            SignageUpdate update = _updates[signage.NetworkID] ?? new SignageUpdate(player, signage, _pluginConfig.SignMessages, (uint)textureIndex, player == null);
            _updates.Remove(signage.NetworkID);
            if (update.IgnoreMessage)
            {
                return;
            }
            
            SendDiscordMessage(update);
        }

        private void OnItemPainted(PaintedItemStorageEntity entity, Item item, BasePlayer player, byte[] image)
        {
            if (entity._currentImageCrc == 0)
            {
                return;
            }
            
            PaintedItemUpdate update = new PaintedItemUpdate(player, entity, item, image, _pluginConfig.SignMessages, false);
            SendDiscordMessage(update);
        }

        private void OnFireworkDesignChanged(PatternFirework firework, ProtoBuf.PatternFirework.Design design, BasePlayer player)
        {
            if (design?.stars == null || design.stars.Count == 0)
            {
                return;
            }
            
            SendDiscordMessage(new FireworkUpdate(player, firework, _pluginConfig.SignMessages));
        }

        private object CanUpdateSign(BasePlayer player, BaseEntity entity)
        {
            if (!_pluginData.IsSignBanned(player))
            {
                return null;
            }

            Chat(player, LangKeys.BlockedMessage, GetFormattedDurationTime(_pluginData.GetRemainingBan(player)));
            
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
            
            Chat(player, LangKeys.BlockedMessage, player, GetFormattedDurationTime(_pluginData.GetRemainingBan(player), player));
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
            
            Chat(player, LangKeys.BlockedMessage, GetFormattedDurationTime(_pluginData.GetRemainingBan(player)));

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
        }

        private void SubscribeAll()
        {
            Subscribe(nameof(OnSignUpdated));
            Subscribe(nameof(OnFireworkDesignChanged));
            Subscribe(nameof(CanUpdateSign));
            Subscribe(nameof(OnFireworkDesignChange));
            
            if (SignArtist != null && SignArtist.IsLoaded)
            {
                Subscribe(nameof(OnPlayerCommand));
                Subscribe(nameof(OnImagePost));
            }
        }
    }
}