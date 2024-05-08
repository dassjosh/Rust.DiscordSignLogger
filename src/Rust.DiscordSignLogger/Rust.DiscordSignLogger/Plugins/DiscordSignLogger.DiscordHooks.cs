using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Rust.SignLogger.Configuration;

namespace Rust.SignLogger.Plugins;

//Define:FileOrder=5
public partial class DiscordSignLogger
{
    [HookMethod(DiscordExtHooks.OnDiscordGuildCreated)]
    private void OnDiscordGuildCreated(DiscordGuild guild)
    {
        bool subscribe = false;
        foreach (SignMessage message in _pluginConfig.SignMessages)
        {
            if (message.MessageChannel == null && message.ChannelId.IsValid())
            {
                DiscordChannel channel = guild.GetChannel(message.ChannelId);
                if (channel != null)
                {
                    message.MessageChannel = channel;
                    subscribe = true;
                }
            }
        }

        if (_pluginConfig.ActionLogChannel.IsValid())
        {
            DiscordChannel channel = guild.GetChannel(_pluginConfig.ActionLogChannel);
            if (channel != null)
            {
                _actionChannel = channel;
            }
        }

        if (subscribe)
        {
            SubscribeAll();
            Puts($"{Title} Ready");
            RegisterApplicationCommands();
        }
    }
}