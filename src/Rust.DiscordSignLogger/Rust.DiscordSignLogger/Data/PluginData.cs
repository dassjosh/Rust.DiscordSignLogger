using System;
using Oxide.Plugins;

namespace Rust.SignLogger.Data;

public class PluginData
{
    public Hash<ulong, DateTime> SignBannedUsers = new();

    public void AddSignBan(ulong player, float duration)
    {
        SignBannedUsers[player] = duration <= 0 ? DateTime.MaxValue : DateTime.UtcNow + TimeSpan.FromSeconds(duration);
    }

    public void RemoveSignBan(ulong player)
    {
        SignBannedUsers.Remove(player);
    }

    public bool IsSignBanned(BasePlayer player) => IsSignBanned(player.userID);
    public bool IsSignBanned(string playerId) => IsSignBanned(ulong.Parse(playerId));

    public bool IsSignBanned(ulong playerId)
    {
        if (!SignBannedUsers.ContainsKey(playerId))
        {
            return false;
        }

        DateTime bannedUntil = SignBannedUsers[playerId];
        if (bannedUntil < DateTime.UtcNow)
        {
            SignBannedUsers.Remove(playerId);
            return false;
        }

        return true;
    }

    public TimeSpan GetRemainingBan(BasePlayer player)
    {
        return SignBannedUsers[player.userID] - DateTime.UtcNow;
    }
}