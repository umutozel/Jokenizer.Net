using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net;

public static class ExtensionMethods {
    private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MethodInfo>>> _extensions = new();

    static ExtensionMethods() => ScanTypes(typeof(Queryable), typeof(Enumerable));

    public static IList<MethodInfo> ProbeAssemblies(params Assembly[] assemblies) =>
        assemblies.SelectMany(ProbeAssembly).ToList();
    public static IList<MethodInfo> ProbeAssembly(Assembly assembly) =>
        ScanTypes(assembly.GetTypes().Where(t => t.IsSealed && t is { IsGenericType: false, IsNested: false }));
    public static IList<MethodInfo> ScanTypes(params Type[] types) => types.SelectMany(ScanType).ToList();
    public static IList<MethodInfo> ScanTypes(IEnumerable<Type> types) => types.SelectMany(ScanType).ToList();

    public static IList<MethodInfo> ScanType(Type type) {
        var extensions = type
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
            .ToList();

        foreach (var method in extensions) {
            var parameters = method.GetParameters();
            var extensionType = parameters[0].ParameterType;

            if (extensionType.ContainsGenericParameters) {
                extensionType = extensionType.GetGenericTypeDefinition();
            }

            var methodName = method.Name;
            _extensions.AddOrUpdate(extensionType,
                _ => new Dictionary<string, List<MethodInfo>> {
                    { methodName, [method] }
                },
                (_, existingDict) => {
                    if (!existingDict.TryGetValue(methodName, out var methodList)) {
                        methodList = [];
                        existingDict[methodName] = methodList;
                    }
                    methodList.Add(method);
                    return existingDict;
                });
        }

        return extensions.ToList();
    }

    internal static IEnumerable<(MethodInfo, IReadOnlyList<ParameterInfo>)> Search(Type forType, string methodName) {
        // find extension methods for implemented interfaces
        IEnumerable<Type> interfaces = forType.GetInterfaces();
        if (forType.IsInterface) {
            interfaces = new [] { forType }.Concat(interfaces);
        }
        foreach (var interfaceType in interfaces) {
            var it = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
            if (!_extensions.TryGetValue(it, out var interfaceMethodDict)) continue;
            if (!interfaceMethodDict.TryGetValue(methodName, out var interfaceMethods)) continue;

            foreach (var method in interfaceMethods) {
                var m = method;
                if (m.IsGenericMethodDefinition) {
                    var genericArgs = interfaceType.GetGenericArguments();
                    if (m.GetGenericArguments().Length != genericArgs.Length) continue;

                    m = m.MakeGenericMethod(genericArgs);
                }

                var prms = m.GetParameters();
                yield return (m, new ArraySegment<ParameterInfo>(prms, 1, prms.Length - 1));
            }
        }

        // find type and BaseType assignable extension methods
        var baseType = forType;
        do {
            var t = baseType.ContainsGenericParameters ? baseType.GetGenericTypeDefinition() : baseType;
            if (_extensions.TryGetValue(t, out var baseMethodDict)
                && baseMethodDict.TryGetValue(methodName, out var baseMethods)) {
                foreach (var m in baseMethods) {
                    var prms = m.GetParameters();
                    yield return (m, new ArraySegment<ParameterInfo>(prms, 1, prms.Length - 1));
                }
            }

            baseType = baseType.BaseType;
        } while (baseType != null);
    }
}
