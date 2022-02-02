using System;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Rust.DiscordSignLogger.Lang;

namespace Rust.DiscordSignLogger.Plugins
{
    //Define:FileOrder=9
    public partial class DiscordSignLogger
    {
        public IPlayer FindPlayerById(string id)
        {
            return covalence.Players.FindPlayerById(id);
        }
        
        public string Lang(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player ? player.UserIDString : null);
        }
        
        public string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player ? player.UserIDString : null), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }

        public void Chat(BasePlayer player, string key) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player)));
        public void Chat(BasePlayer player, string key, params object[] args) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player, args)));
        
        public void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);
        public void SaveButtonData() => Interface.Oxide.DataFileSystem.WriteObject(Name + "_Buttons", _buttonData);
    }
}