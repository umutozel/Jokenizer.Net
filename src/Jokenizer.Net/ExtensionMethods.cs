using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net {

    public static class ExtensionMethods {
        private static List<Func<Type, string, int, MethodInfo>> finders = new List<Func<Type, string, int, MethodInfo>>();
        private static List<MethodInfo> queryableExtensions;
        private static List<MethodInfo> enumerableExtensions;

        static ExtensionMethods() {
            queryableExtensions = GetExtensionMethods(typeof(Queryable)).ToList();
            enumerableExtensions = GetExtensionMethods(typeof(Enumerable)).ToList();

            finders.Add(QueryableExtensionFinder);
            finders.Add(EnumerableExtensionFinder);
        }

        public static IEnumerable<MethodInfo> GetExtensionMethods(Type type) {
            return type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false));
        }

        public static void AddExtensionFinder(Func<Type, string, int, MethodInfo> finder) {
            finders.Add(finder);
        }

        public static MethodInfo GetExtensionMethod(Type forType, string name, int parameterCount) {
            return finders.Select(f => f(forType, name, parameterCount)).FirstOrDefault(v => v != null);
        }

        private static MethodInfo QueryableExtensionFinder(Type forType, string name, int parameterCount) {
            return FindExtension(queryableExtensions, forType, name, parameterCount);
        }

        private static MethodInfo EnumerableExtensionFinder(Type forType, string name, int parameterCount) {
            return FindExtension(enumerableExtensions, forType, name, parameterCount);
        }

        private static MethodInfo FindExtension(IEnumerable<MethodInfo> extensions, Type forType, string name, int parameterCount) {
            if (!forType.IsConstructedGenericType) return null;

            var args = forType.GetGenericArguments();
            if (args.Length != 1) return null;

            var extension = extensions.FirstOrDefault(m => {
                if (m.Name != name) return false;

                if (m.IsGenericMethodDefinition) {
                    m = m.MakeGenericMethod(args);
                }

                var prms = m.GetParameters();
                return prms.Length == parameterCount + 1 && prms[0].ParameterType.IsAssignableFrom(forType);
            });

            if (extension != null && extension.IsGenericMethodDefinition) {
                extension = extension.MakeGenericMethod(args);
            }

            return extension;
        }
    }
}
