using System;
using Rust.SignLogger.Configuration;

namespace Rust.SignLogger.Data
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