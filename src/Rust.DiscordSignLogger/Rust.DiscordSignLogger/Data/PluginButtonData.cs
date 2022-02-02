using System;
using System.Collections.Generic;
using Oxide.Plugins;
using Rust.DiscordSignLogger.Configuration;

namespace Rust.DiscordSignLogger.Data
{
    public class PluginButtonData
    {
        public Hash<int, ButtonData> CommandLookup = new Hash<int, ButtonData>();

        public void AddOrUpdate(ImageMessageButtonCommand command)
        {
            CommandLookup[command.CommandId] = new ButtonData(command);
        }

        public ImageMessageButtonCommand Get(int hash)
        {
            return CommandLookup[hash]?.Command;
        }

        public void CleanupExpired(List<int> active, float deleteAfter)
        {
            List<int> oldButtons = new List<int>();
            foreach (KeyValuePair<int, ButtonData> button in CommandLookup)
            {
                if (active.Contains(button.Key))
                {
                    continue;
                }

                if ((DateTime.UtcNow - button.Value.AddedDate).TotalDays >= deleteAfter)
                {
                    oldButtons.Add(button.Key);
                }
            }

            for (int index = 0; index < oldButtons.Count; index++)
            {
                int button = oldButtons[index];
                CommandLookup.Remove(button);
            }
        }
    }
}