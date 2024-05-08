using System;
using Newtonsoft.Json;
using Rust.SignLogger.Json;

namespace Rust.SignLogger.Ids;

[JsonConverter(typeof(ButtonIdConverter))]
public readonly struct ButtonId : IEquatable<ButtonId>
{
    public readonly string Id;

    public ButtonId(string id)
    {
        Id = id;
    }

    public bool Equals(ButtonId other) => Id == other.Id;

    public override bool Equals(object obj) => obj is ButtonId other && Equals(other);

    public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;
}