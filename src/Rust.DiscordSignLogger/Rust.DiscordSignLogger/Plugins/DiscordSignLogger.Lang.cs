using System;
using System.Collections.Generic;
using Oxide.Ext.Discord.Libraries;
using Rust.SignLogger.Lang;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=10
public partial class DiscordSignLogger
{
    protected override void LoadDefaultMessages()
    {
        lang.RegisterMessages(new Dictionary<string, string>
        {
            [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
            [LangKeys.NoPermission] = "You do not have permission to perform this action",
            [LangKeys.KickReason] = "Inappropriate sign/firework image",
            [LangKeys.BanReason] = "Inappropriate sign/firework image",
            [LangKeys.BlockedMessage] = $"You're not allowed to update this sign/firework because you have been blocked. Your block will expire in {DefaultKeys.Timespan.Formatted}.",
        }, this);
            
        lang.RegisterMessages(new Dictionary<string, string>
        {
            [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
            [LangKeys.NoPermission] = "У вас нет разрешения на выполнение этого действия",
            [LangKeys.KickReason] = "Недопустимое изображение знака/фейерверка",
            [LangKeys.BanReason] = "Недопустимое изображение знака/фейерверка",
            [LangKeys.BlockedMessage] = $"Возможность использовать изображения на знаке/феерверке для вас заблокирована. Разблокировка через {DefaultKeys.Timespan.Formatted}.",
        }, this, "ru");
    }
    
    public string Lang(string key, BasePlayer player = null)
    {
        return lang.GetMessage(key, this, player ? player.UserIDString : null);
    }
    
    public string Lang(string key, BasePlayer player, PlaceholderData data)
    {
        return _placeholders.ProcessPlaceholders(Lang(key, player), data);
    }
        
    public string Lang(string key, BasePlayer player = null, params object[] args)
    {
        try
        {
            return string.Format(Lang(key, player), args);
        }
        catch (Exception ex)
        {
            PrintError($"Lang Key '{key}' threw exception\n:{ex}");
            throw;
        }
    }

    public void Chat(BasePlayer player, string key) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player)));
    public void Chat(BasePlayer player, string key, PlaceholderData data) => PrintToChat(player, Lang(LangKeys.Chat, player, Lang(key, player, data)));
}