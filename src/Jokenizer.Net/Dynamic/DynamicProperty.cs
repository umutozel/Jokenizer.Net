using System;

namespace Jokenizer.Net.Dynamic;

internal class DynamicProperty(string name, Type type) {
    public string Name { get; } = name;
    public Type Type { get; } = type;
}
