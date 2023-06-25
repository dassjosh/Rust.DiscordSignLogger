using Oxide.Ext.Discord.Interfaces;
using Oxide.Plugins;

namespace Rust.SignLogger.Plugins
{
    //Define:FileOrder=1
    [Info("Discord Sign Logger", "MJSU", "1.1.0")]
    [Description("Logs Sign / Firework Changes To Discord")]
    public partial class DiscordSignLogger : RustPlugin, IDiscordPlugin
    {
        
    }
}