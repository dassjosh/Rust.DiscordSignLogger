using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Rust.SignLogger.Configuration;
using Rust.SignLogger.Interfaces;
using Rust.SignLogger.Plugins;

namespace Rust.SignLogger.Updates
{
    public abstract class BaseImageUpdate : ILogEvent
    {
        public IPlayer Player { get; }
        public ulong PlayerId { get; }
        public string DisplayName { get; }
        public BaseEntity Entity { get; }
        public List<SignMessage> Messages { get; }
        public bool IgnoreMessage { get; }
        public int ItemId { get; protected set; }
        
        public uint TextureIndex { get; protected set; }
        public abstract bool SupportsTextureIndex { get; }

        protected BaseImageUpdate(BasePlayer player, BaseEntity entity, List<SignMessage> messages, bool ignoreMessage)
        {
            Player = player.IPlayer;
            DisplayName = player.displayName;
            PlayerId = player.userID;
            Entity = entity;
            Messages = messages;
            IgnoreMessage = ignoreMessage;
        }

        public abstract byte[] GetImage();
    }
}