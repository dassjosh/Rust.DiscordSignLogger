using Oxide.Ext.Discord.Libraries;

namespace Rust.SignLogger.Templates;

public class TemplateKeys
{
    public static readonly TemplateKey NoPermission = new(nameof(NoPermission));
    
    public static class Action
    {
        private const string Base = nameof(Action) + ".";
                
        public static readonly TemplateKey Message = new(Base + nameof(Message));
        public static readonly TemplateKey Button = new(Base + nameof(Button));
    }
    
    public static class Errors
    {
        private const string Base = nameof(Errors) + ".";
                
        public static readonly TemplateKey FailedToParse = new(Base + nameof(FailedToParse));
        public static readonly TemplateKey ButtonIdNotFound = new(Base + nameof(ButtonIdNotFound));
    }

    public static class Commands
    {
        private const string Base = nameof(Commands) + ".";

        public static class Block
        {
            private const string Base = Commands.Base + nameof(Block) + ".";

            public static readonly TemplateKey Success = new(Base + nameof(Success));

            public static class Errors
            {
                private const string Base = Block.Base + nameof(Errors) + ".";
                
                public static readonly TemplateKey PlayerNotFound = new(Base + nameof(PlayerNotFound));
                public static readonly TemplateKey IsAlreadyBanned = new(Base + nameof(IsAlreadyBanned));
            }
        }
        
        public static class Unblock
        {
            private const string Base = Commands.Base + nameof(Unblock) + ".";

            public static readonly TemplateKey Success = new(Base + nameof(Success));
            
            public static class Errors
            {
                private const string Base = Unblock.Base + nameof(Errors) + ".";
                
                public static readonly TemplateKey PlayerNotFound = new(Base + nameof(PlayerNotFound));
                public static readonly TemplateKey NotBanned = new(Base + nameof(NotBanned));
            }
        }
    }
}