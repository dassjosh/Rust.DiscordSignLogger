namespace Rust.DiscordSignLogger.Lang
{
    public static class LangKeys
    {
        public const string Chat = nameof(Chat);
        public const string NoPermission = nameof(NoPermission);
        public const string KickReason = nameof(KickReason);
        public const string BanReason = nameof(BanReason);
        public const string BlockedMessage = nameof(BlockedMessage);
        public const string ActionMessage = nameof(ActionMessage);
        public const string DeletedLog = nameof(DeletedLog);
        public const string DeletedButtonCache = nameof(DeletedButtonCache);
        public const string SignArtistTitle = nameof(SignArtistTitle);
        public const string SignArtistValue = nameof(SignArtistValue);
        
        public static class Format
        {
            private const string Base = nameof(Format) + ".";
            public const string Days = Base + nameof(Days);
            public const string Hours = Base + nameof(Hours);
            public const string Minutes = Base + nameof(Minutes);
            public const string Day = Base + nameof(Day);
            public const string Hour = Base + nameof(Hour);
            public const string Minute = Base + nameof(Minute);
            public const string Second = Base + nameof(Second);
            public const string Seconds = Base + nameof(Seconds);
            public const string TimeField = Base + nameof(TimeField);
        }
    }
}