using Oxide.Core.Libraries.Covalence;

namespace Rust.DiscordSignLogger.Interfaces
{
    public interface ILogEvent
    {
        IPlayer Player { get; }
        BaseEntity Entity { get; }
        uint TextureIndex { get; }
    }
}