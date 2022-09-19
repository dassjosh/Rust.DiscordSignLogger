using System;
using System.Text;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;
using Rust.SignLogger.Lang;
using Rust.SignLogger.Updates;
using UnityEngine;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=11
    public partial class DiscordSignLogger
    {
#pragma warning disable CS0649
        [PluginReference] private Plugin PlaceholderAPI;
#pragma warning restore CS0649
        private Action<IPlayer, StringBuilder, bool> _replacer;
        
        private string ParsePlaceholders(IPlayer player, string field)
        {
            _sb.Clear();
            _sb.Append(field);
            GetReplacer()?.Invoke(player, _sb, false);
            if (_sb.Length == 0)
            {
                _sb.Append("\u200b");
            }
            return _sb.ToString();
        }
        
        private void ParsePlaceholders(IPlayer player, StringBuilder sb)
        {
            GetReplacer()?.Invoke(player, sb, false);
        }
        
        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin?.Name == "PlaceholderAPI")
            {
                _replacer = null;
            }
        }

        public string GetSignAristUrl()
        {
            SignageUpdate signage = _log as SignageUpdate;
            return signage?.Url ?? string.Empty;
        }
        
        private void OnPlaceholderAPIReady()
        {
            RegisterPlaceholder("dsl.entity.id", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                return entity ? entity.net?.ID ?? 0 : 0;

            }, "Displays the entity ID");
            
            RegisterPlaceholder("dsl.entity.textureindex", (player, s) => _log?.TextureIndex ?? 0, "Displays the texture index");
            
            RegisterPlaceholder("dsl.entity.name", (player, s) =>
            {
                if (_log.ItemId != 0)
                {
                    return GetItemName(_log.ItemId);
                }
                
                BaseEntity entity = _log.Entity;
                return entity ? GetEntityName(entity) : "Entity Not Found";
            }, "Displays the entity item name");
            
            RegisterPlaceholder("dsl.entity.owner.id", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                return entity ? entity.OwnerID : 0;
            }, "Displays the entity Owner ID");
            
            RegisterPlaceholder("dsl.entity.owner.name", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                if (!entity)
                {
                    return "Unknown";
                }

                IPlayer owner = covalence.Players.FindPlayerById(entity.OwnerID.ToString());
                if (owner == null)
                {
                    return "Unknown";
                }

                BasePlayer ownerPlayer = (BasePlayer)owner.Object;
                return ownerPlayer ? ownerPlayer.displayName : owner.Name;
            }, "Displays the entity owner player name");

            RegisterPlaceholder("dsl.entity.position", (player, s) =>
            {
                BaseEntity entity = _log.Entity;
                if (!entity)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        return Vector3.zero;
                    }

                    return 0f;
                }

                Vector3 pos = entity.transform.position;
                if (string.IsNullOrEmpty(s))
                {
                    return pos;
                }

                if (s.Equals("x", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.x;
                }
                
                if (s.Equals("y", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.y;
                }
                
                if (s.Equals("z", StringComparison.OrdinalIgnoreCase))
                {
                    return pos.z;
                }

                return pos;
            }, "Displays the position of the entity");
            
            RegisterPlaceholder("dsl.discord.user.id", (player, s) => _activeMember?.Id ?? default(Snowflake), "Discord user id who clicked the button");
            RegisterPlaceholder("dsl.discord.user.name", (player, s) => _activeMember?.DisplayName ?? "Unknown User", "Discord display name of user who clicked the button");
            RegisterPlaceholder("dsl.kick.reason", (player, s) => Lang(LangKeys.KickReason), "Kick Reason Lang Value");
            RegisterPlaceholder("dsl.ban.reason", (player, s) => Lang(LangKeys.BanReason), "Ban Reason Lang Value");
            RegisterPlaceholder("dsl.signartist.url", (player, s) => GetSignAristUrl(), "Sign Artist URL");
            RegisterPlaceholder("dsl.action.guild.id", (player, s) => _interaction?.GuildId ?? default(Snowflake), "Actioned Message Guild ID");
            RegisterPlaceholder("dsl.action.channel.id", (player, s) => _interaction?.ChannelId ?? default(Snowflake), "Actioned Message Channel ID");
            RegisterPlaceholder("dsl.action.message.id", (player, s) => _interaction.Message?.Id ?? default(Snowflake), "Actioned Message Message ID");
        }
        
        private void RegisterPlaceholder(string key, Func<IPlayer, string, object> action, string description = null)
        {
            if (IsPlaceholderApiLoaded())
            {
                PlaceholderAPI.Call("AddPlaceholder", this, key, action, description);
            }
        }

        private Action<IPlayer, StringBuilder, bool> GetReplacer()
        {
            if (!IsPlaceholderApiLoaded())
            {
                return _replacer;
            }
            
            return _replacer ?? (_replacer = PlaceholderAPI.Call<Action<IPlayer, StringBuilder, bool>>("GetProcessPlaceholders", 1));
        }

        private bool IsPlaceholderApiLoaded() => PlaceholderAPI != null && PlaceholderAPI.IsLoaded;
    }
}