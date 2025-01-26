using System;
using System.Collections.Generic;
using System.Linq;

namespace Jokenizer.Net.Dynamic;

internal class Signature : IEquatable<Signature> {
    private readonly int _hashCode;

    public Signature(IEnumerable<DynamicProperty> properties) {
        Properties = properties.ToArray();
        _hashCode = 0;

        foreach (var p in Properties) {
            _hashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
        }
    }

    public DynamicProperty[] Properties { get; }

    public override int GetHashCode() => _hashCode;

    public bool Equals(Signature other) =>
        Properties.Length == other.Properties.Length
        && Properties.All(p => other.Properties.Any(o => o.Name == p.Name && o.Type == p.Type));
}
