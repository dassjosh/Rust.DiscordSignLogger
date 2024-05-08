using Oxide.Ext.Discord.Interfaces;

namespace Rust.SignLogger.Ids;

public readonly struct StateKey : IDiscordKey
{
    public readonly string State;

    public StateKey(string state)
    {
        State = state;
    }

    public override string ToString() => State;
}