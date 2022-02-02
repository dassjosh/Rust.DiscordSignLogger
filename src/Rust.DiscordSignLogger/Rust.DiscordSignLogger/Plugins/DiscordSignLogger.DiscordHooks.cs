using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Rust.DiscordSignLogger.Configuration;

namespace Rust.DiscordSignLogger.Plugins
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
                if (message.MessageChannel == null && message.ChannelId.IsValid() && guild.Channels.ContainsKey(message.ChannelId))
                {
                    message.MessageChannel = guild.Channels[message.ChannelId];
                    subscribe = true;
                }
            }

            if (_pluginConfig.ActionLog.Channel.IsValid() && guild.Channels.ContainsKey(_pluginConfig.ActionLog.Channel))
            {
                _actionChannel = guild.Channels[_pluginConfig.ActionLog.Channel];
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