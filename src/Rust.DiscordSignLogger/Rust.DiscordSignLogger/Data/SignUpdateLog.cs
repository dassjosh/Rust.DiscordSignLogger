using System;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Rust.SignLogger.Interfaces;
using Rust.SignLogger.Plugins;
using Rust.SignLogger.Updates;

namespace Rust.SignLogger.Data
{
    public class SignUpdateLog : ILogEvent
    {
        public ulong PlayerId { get; set; }
        public uint EntityId { get; set; }
        public int ItemId { get; set; }
        public uint TextureIndex { get; set; }

        public DateTime LogDate { get; set; }

        [JsonIgnore]
        private IPlayer _player;
        [JsonIgnore]
        public IPlayer Player => _player ?? (_player = DiscordSignLogger.Instance.FindPlayerById(PlayerId.ToString()));

        [JsonIgnore]
        private BaseEntity _entity;
        
        [JsonIgnore]
        public BaseEntity Entity
        {
            get
            {
                if (_entity)
                {
                    return _entity;
                }
                
                if (LogDate < SaveRestore.SaveCreatedTime)
                {
                    return null;
                }

                _entity = BaseNetworkable.serverEntities.Find(EntityId) as BaseEntity;
                
                return _entity;
            }
        }
        
        [JsonConstructor]
        public SignUpdateLog()
        {
            
        }

        public SignUpdateLog(BaseImageUpdate update)
        {
            PlayerId = update.PlayerId;
            EntityId = update.Entity.net.ID;
            LogDate = DateTime.UtcNow;

            if (update.SupportsTextureIndex)
            {
                TextureIndex = update.TextureIndex;
            }

            if (update is PaintedItemUpdate)
            {
                ItemId = ((PaintedItemUpdate)update).ItemId;
            }
        }
    }
}