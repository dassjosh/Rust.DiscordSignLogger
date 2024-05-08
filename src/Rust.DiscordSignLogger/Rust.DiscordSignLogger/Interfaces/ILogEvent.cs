using Oxide.Core.Libraries.Covalence;

namespace Rust.SignLogger.Interfaces;

public interface ILogEvent
{
    IPlayer Player { get; }
    BaseEntity Entity { get; }
    int ItemId { get; }
    byte TextureIndex { get; }
}