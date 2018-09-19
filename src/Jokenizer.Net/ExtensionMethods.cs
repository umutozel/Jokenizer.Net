using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net {

    public static class ExtensionMethods {
        private static HashSet<MethodInfo> extensions = new HashSet<MethodInfo>();

        static ExtensionMethods() {
            ScanTypes(typeof(Queryable), typeof(Enumerable));
        }

        public static IList<MethodInfo> ProbeAllAssemblies() {
            return ProbeAssemblies(Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load));
        }

        public static IList<MethodInfo> ProbeAssemblies(params Assembly[] assemblies) => assemblies.SelectMany(ProbeAssembly).ToList();
        public static IList<MethodInfo> ProbeAssemblies(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(ProbeAssembly).ToList();

        public static IList<MethodInfo> ProbeAssembly(Assembly assembly) {
            return ScanTypes(assembly.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested));
        }

        public static IList<MethodInfo> ScanTypes(params Type[] types) => types.SelectMany(ScanType).ToList();
        public static IList<MethodInfo> ScanTypes(IEnumerable<Type> types) => types.SelectMany(ScanType).ToList();

        public static IList<MethodInfo> ScanType(Type type) {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
                .ToList();

            methods.ForEach(m => extensions.Add(m));

            return methods;
        }

        public static MethodInfo GetExtensionMethod(Type forType, string name, int parameterCount) {
            var args = forType.IsConstructedGenericType ? forType.GetGenericArguments() : null;

            return extensions.Select(m => {
                if (m.Name != name) return null;

                if (m.IsGenericMethodDefinition) {
                    m = m.MakeGenericMethod(args);
                }

                var prms = m.GetParameters();
                return prms.Length == parameterCount + 1 && prms[0].ParameterType.IsAssignableFrom(forType) ? m : null;
            }).FirstOrDefault(m => m != null);
        }
    }
}
