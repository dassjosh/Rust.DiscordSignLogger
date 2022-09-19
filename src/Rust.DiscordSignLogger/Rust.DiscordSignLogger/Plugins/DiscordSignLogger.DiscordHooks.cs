using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Rust.SignLogger.Configuration;

namespace Rust.SignLogger.Plugins
{
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
                    DiscordChannel channel = GetChannel(guild, message.ChannelId);
                    if (channel != null)
                    {
                        message.MessageChannel = channel;
                        subscribe = true;
                    }
                }
            }

            if (_pluginConfig.ActionLog.ChannelId.IsValid())
            {
                DiscordChannel channel = GetChannel(guild, _pluginConfig.ActionLog.ChannelId);
                if (channel != null)
                {
                    _actionChannel = channel;
                }
            }

            if (subscribe)
            {
                SubscribeAll();
                Puts($"{Title} Ready");
            }
        }

        [HookMethod(DiscordExtHooks.OnDiscordInteractionCreated)]
        private void OnDiscordInteractionCreated(DiscordInteraction interaction)
        {
            switch (interaction.Type)
            {
                case InteractionType.MessageComponent:
                    HandleMessageComponentCommand(interaction);
                    break;
            }
        }
    }
}