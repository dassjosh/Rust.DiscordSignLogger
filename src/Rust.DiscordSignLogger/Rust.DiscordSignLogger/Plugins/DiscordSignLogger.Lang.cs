using System;
using System.Collections.Generic;
using System.Text;
using Rust.SignLogger.Lang;

namespace Rust.SignLogger.Plugins
{
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
                [LangKeys.BlockedMessage] = "You're not allowed to update this sign/firework because you have been blocked. Your block will expire in {0}.",
                [LangKeys.ActionMessage] = $"[{Title}] <@{{dsl.discord.user.id}}> ran command \"{{dsl.command}}\"",
                [LangKeys.DeletedLog] = "The log data for this message was not found. If it's older than {0} days then it may have been deleted.",
                [LangKeys.DeletedButtonCache] = "Button was not found in cache. If this message is older than {0} days then it may have been deleted.",
                [LangKeys.SignArtistTitle] = "Sign Artist URL:",
                [LangKeys.SignArtistValue] = "{dsl.signartist.url}",
                [LangKeys.Format.Day] = "day ",
                [LangKeys.Format.Days] = "days ",
                [LangKeys.Format.Hour] = "hour ",
                [LangKeys.Format.Hours] = "hours ",
                [LangKeys.Format.Minute] = "minute ",
                [LangKeys.Format.Minutes] = "minutes ",
                [LangKeys.Format.Second] = "second",
                [LangKeys.Format.Seconds] = "seconds",
                [LangKeys.Format.TimeField] = $"<color={AccentColor}>{{0}}</color> {{1}}"

            }, this);
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.Chat] = $"<color=#bebebe>[<color={AccentColor}>{Title}</color>] {{0}}</color>",
                [LangKeys.NoPermission] = "У вас нет разрешения на выполнение этого действия",
                [LangKeys.KickReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BanReason] = "Недопустимое изображение знака/фейерверка",
                [LangKeys.BlockedMessage] = "Возможность использовать изображения на знаке/феерверке для вас заблокирована. Разблокировка через {0}.",
                [LangKeys.ActionMessage] = $"[{Title}] <@{{dsl.discord.user.id}}> выполнил команду \"{{dsl.command}}\"",
                [LangKeys.DeletedLog] = "Данные журнала для этого сообщения не найдены. Если сообщение старше {0} дней, возможно, оно было удалено.",
                [LangKeys.DeletedButtonCache] = "Кнопка не найдена в кеше. Если сообщение старше {0} дней, возможно, оно было удалено.",
                [LangKeys.SignArtistTitle] = "Sign Artist URL:",
                [LangKeys.SignArtistValue] = "{dsl.signartist.url}",
                [LangKeys.Format.Day] = "день ",
                [LangKeys.Format.Days] = "дней ",
                [LangKeys.Format.Hour] = "час ",
                [LangKeys.Format.Hours] = "часов ",
                [LangKeys.Format.Minute] = "минуту ",
                [LangKeys.Format.Minutes] = "минут ",
                [LangKeys.Format.Second] = "секунду",
                [LangKeys.Format.Seconds] = "секунд",
                [LangKeys.Format.TimeField] = $"<color={AccentColor}>{{0}}</color> {{1}}"
            }, this, "ru");
        }

        private string GetFormattedDurationTime(TimeSpan time, BasePlayer player = null)
        {
            _sb.Clear();

            if (time.TotalDays >= 1)
            {
                BuildTime(_sb, time.Days == 1 ? LangKeys.Format.Day : LangKeys.Format.Days, player, time.Days);
            }

            if (time.TotalHours >= 1)
            {
                BuildTime(_sb, time.Hours == 1 ? LangKeys.Format.Hour : LangKeys.Format.Hours, player, time.Hours);
            }

            if (time.TotalMinutes >= 1)
            {
                BuildTime(_sb, time.Minutes == 1 ? LangKeys.Format.Minute : LangKeys.Format.Minutes, player, time.Minutes);
            }

            BuildTime(_sb, time.Seconds == 1 ? LangKeys.Format.Second : LangKeys.Format.Seconds, player, time.Seconds);

            return _sb.ToString();
        }

        private void BuildTime(StringBuilder sb, string key, BasePlayer player, int value)
        {
            sb.Append(Lang(LangKeys.Format.TimeField, player, value, Lang(key, player)));
        }
    }
}