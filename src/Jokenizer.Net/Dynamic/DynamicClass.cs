using System.Reflection;
using System.Text;

namespace Jokenizer.Net.Dynamic;

public abstract class DynamicClass {

    public override string ToString() {
        var props = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var sb = new StringBuilder();
        sb.Append("{");

        for (var i = 0; i < props.Length; i++) {
            if (i > 0) {
                sb.Append(", ");
            }
            sb.Append(props[i].Name);
            sb.Append("=");
            sb.Append(props[i].GetValue(this, null));
        }

        sb.Append("}");
        return sb.ToString();
    }
}
