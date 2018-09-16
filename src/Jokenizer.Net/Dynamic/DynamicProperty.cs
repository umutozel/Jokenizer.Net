using System;

namespace Jokenizer.Net.Dynamic {

    internal class DynamicProperty {

        public DynamicProperty(string name, Type type) {
            if (name == null) throw new ArgumentNullException("name");
            if (type == null) throw new ArgumentNullException("type");

            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }

        public Type Type { get; }
    }
}
