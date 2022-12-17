using System;
using System.Collections.Generic;
using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;

namespace Rust.SignLogger.Data
{
    public class PluginData
    {
        public Hash<Snowflake, SignUpdateLog> SignLogs = new Hash<Snowflake, SignUpdateLog>();
        public Hash<ulong, DateTime> SignBannedUsers = new Hash<ulong, DateTime>();

        public SignUpdateLog GetLog(Snowflake messageId)
        {
            return SignLogs[messageId];
        }

        public void AddLog(Snowflake messageId, SignUpdateLog data)
        {
            SignLogs[messageId] = data;
        }

        public void CleanupExpired(float deleteAfter)
        {
            List<Snowflake> cleanup = new List<Snowflake>();
            foreach (KeyValuePair<Snowflake, SignUpdateLog> log in SignLogs)
            {
                if ((DateTime.UtcNow - log.Value.LogDate).TotalDays >= deleteAfter)
                {
                    cleanup.Add(log.Key);
                }
            }

            foreach (Snowflake key in cleanup)
            {
                SignLogs.Remove(key);
            }
        }

        public void AddSignBan(ulong player, float duration)
        {
            SignBannedUsers[player] = duration <= 0 ? DateTime.MaxValue : DateTime.UtcNow + TimeSpan.FromSeconds(duration);
        }

        public void RemoveSignBan(ulong player)
        {
            SignBannedUsers.Remove(player);
        }

        public bool IsSignBanned(BasePlayer player)
        {
            if (!SignBannedUsers.ContainsKey(player.userID))
            {
                return false;
            }

            DateTime bannedUntil = SignBannedUsers[player.userID];
            if (bannedUntil < DateTime.UtcNow)
            {
                SignBannedUsers.Remove(player.userID);
                return false;
            }

            return true;
        }

        public TimeSpan GetRemainingBan(BasePlayer player)
        {
            return SignBannedUsers[player.userID] - DateTime.UtcNow;
        }
    }
}