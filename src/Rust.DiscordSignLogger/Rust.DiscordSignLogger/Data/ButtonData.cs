using System;
using Rust.DiscordSignLogger.Configuration;

namespace Rust.DiscordSignLogger.Data
{
    public class ButtonData
    {
        public ImageMessageButtonCommand Command { get; set; }
        public DateTime AddedDate { get; set; }

        public ButtonData(ImageMessageButtonCommand command)
        {
            Command = command;
            AddedDate = DateTime.UtcNow;
        }
    }
}