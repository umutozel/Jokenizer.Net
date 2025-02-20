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
            interfaces = new[] { forType }.Concat(interfaces);
        }

        foreach (var interfaceType in interfaces) {
            var interfaceMethods = Find(interfaceType, methodName);
            foreach (var method in interfaceMethods) {
                if (!FixGeneric(interfaceType, method, out var m))
                    continue;

                var prms = m.GetParameters();
                yield return (m, new ArraySegment<ParameterInfo>(prms, 1, prms.Length - 1));
            }
        }

        // find type and BaseType assignable extension methods
        var type = forType;
        do {
            var typeMethods = Find(type, methodName);
            foreach (var method in typeMethods) {
                if (!FixGeneric(type, method, out var m))
                    continue;

                var prms = m.GetParameters();
                yield return (m, new ArraySegment<ParameterInfo>(prms, 1, prms.Length - 1));
            }

            type = type.BaseType;
        } while (type != null);
    }

    private static IEnumerable<MethodInfo> Find(Type type, string methodName) {
        var exactMatches = FindForType(type, methodName);

        if (!type.IsConstructedGenericType)
            return exactMatches;

        var genDef = type.GetGenericTypeDefinition();
        var genDefMatches = FindForType(genDef, methodName);

        return exactMatches.Concat(genDefMatches);
    }

    private static IEnumerable<MethodInfo> FindForType(Type type, string methodName) {
        if (_extensions.TryGetValue(type, out var interfaceMethodDict) &&
            interfaceMethodDict.TryGetValue(methodName, out var interfaceMethods)) {
            return interfaceMethods;
        }

        return [];
    }

    private static bool FixGeneric(Type type, MethodInfo method, out MethodInfo fixedMethod) {
        fixedMethod = method;
        if (!method.IsGenericMethodDefinition) return true;

        var genericArgs = type.GetGenericArguments();
        if (method.GetGenericArguments().Length != genericArgs.Length)
            return false;

        fixedMethod = method.MakeGenericMethod(genericArgs);

        return true;
    }
}
