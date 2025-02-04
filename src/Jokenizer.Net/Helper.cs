using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net {

    public static class Helper {

        public static bool IsSuitable(ParameterInfo[] prms, Expression?[] args) {
            if (prms.Length != args.Length) return false;

            for (var i = 0; i < prms.Length; i++) {
                var prm = prms[i];
                var arg = args[i];

                if (arg == null) {
                    if (!typeof(Delegate).IsAssignableFrom(prm.ParameterType))
                        return false;
                }
                else if (prm.ParameterType != arg.Type)
                    return false;
            }

            return true;
        }
    }
}
