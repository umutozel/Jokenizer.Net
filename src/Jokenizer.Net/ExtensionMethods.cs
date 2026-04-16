using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net;

public static class ExtensionMethods {
    private static readonly ConcurrentDictionary<Type, Dictionary<string, List<MethodInfo>>> _extensions = new();
    private static readonly HashSet<Assembly> _scannedAssemblies = [];
    private static readonly HashSet<Type> _scannedTypes = [];
    private static readonly object _lock = new();

    static ExtensionMethods() => ScanTypes(typeof(Queryable), typeof(Enumerable));

    public static IList<MethodInfo> ProbeAssemblies(params Assembly[] assemblies) =>
        assemblies.SelectMany(ProbeAssembly).ToList();

    public static IList<MethodInfo> ProbeAssembly(Assembly assembly) {
        if (assembly.IsDynamic)
            return [];

        // Hold the lock across the full scan so concurrent callers wait until the assembly's
        // extensions are fully published, rather than racing past a "scanned" marker.
        lock (_lock) {
            if (!_scannedAssemblies.Add(assembly))
                return [];

            Type[] types;
            try {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex) {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            return ScanTypes(types.Where(t => t is { IsSealed: true, IsGenericType: false, IsNested: false }));
        }
    }

    public static IList<MethodInfo> RegisterAssembly(Assembly assembly) => ProbeAssembly(assembly);

    public static IList<MethodInfo> RegisterType(Type type) => ScanType(type);

    public static IList<MethodInfo> ScanTypes(params Type[] types) => types.SelectMany(ScanType).ToList();
    public static IList<MethodInfo> ScanTypes(IEnumerable<Type> types) => types.SelectMany(ScanType).ToList();

    public static IList<MethodInfo> ScanType(Type type) {
        lock (_lock) {
            if (!_scannedTypes.Add(type))
                return [];

            var extensions = type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
                .ToList();

            // Group incoming methods by their extension target type so we can publish each target's
            // updated inner dictionary atomically (copy-on-write).
            var pending = new Dictionary<Type, Dictionary<string, List<MethodInfo>>>();
            foreach (var method in extensions) {
                var parameters = method.GetParameters();
                if (parameters.Length == 0) continue;

                var extensionType = parameters[0].ParameterType;
                if (extensionType.IsGenericType && extensionType.ContainsGenericParameters) {
                    extensionType = extensionType.GetGenericTypeDefinition();
                }

                if (!pending.TryGetValue(extensionType, out var methodDict)) {
                    methodDict = new Dictionary<string, List<MethodInfo>>();
                    pending[extensionType] = methodDict;
                }
                if (!methodDict.TryGetValue(method.Name, out var methodList)) {
                    methodList = [];
                    methodDict[method.Name] = methodList;
                }
                methodList.Add(method);
            }

            // Publish each extension-type's methods via copy-on-write so concurrent readers always
            // observe either the pre-scan or the fully-merged snapshot — never a mid-mutation list.
            foreach (var kvp in pending) {
                var extensionType = kvp.Key;
                var newMethods = kvp.Value;
                _extensions.AddOrUpdate(extensionType,
                    _ => newMethods,
                    (_, existingDict) => {
                        var merged = new Dictionary<string, List<MethodInfo>>(existingDict);
                        foreach (var entry in newMethods) {
                            if (merged.TryGetValue(entry.Key, out var existingList)) {
                                var combined = new List<MethodInfo>(existingList.Count + entry.Value.Count);
                                combined.AddRange(existingList);
                                combined.AddRange(entry.Value);
                                merged[entry.Key] = combined;
                            } else {
                                merged[entry.Key] = entry.Value;
                            }
                        }
                        return merged;
                    });
            }

            return extensions.ToList();
        }
    }

    private static void EnsureAssemblyScanned(Type? type) {
        var asm = type?.Assembly;
        if (asm == null || asm.IsDynamic) return;

        // ProbeAssembly is fully-locked; concurrent callers wait here until it completes and
        // any published extensions are visible to this thread.
        ProbeAssembly(asm);
    }

    internal sealed class ExtensionCandidate {
        public ExtensionCandidate(MethodInfo method, IReadOnlyList<ParameterInfo> rawParameters,
                                  IReadOnlyList<Type> parameterTypes, Dictionary<Type, Type> substitution) {
            Method = method;
            RawParameters = rawParameters;
            ParameterTypes = parameterTypes;
            Substitution = substitution;
        }

        public MethodInfo Method { get; }
        public IReadOnlyList<ParameterInfo> RawParameters { get; }
        public IReadOnlyList<Type> ParameterTypes { get; }
        public Dictionary<Type, Type> Substitution { get; }
    }

    internal static IEnumerable<ExtensionCandidate> Search(Type forType, string methodName) {
        EnsureAssemblyScanned(forType);

        IEnumerable<Type> interfaces = forType.GetInterfaces();
        if (forType.IsInterface) {
            interfaces = new[] { forType }.Concat(interfaces);
        }

        foreach (var interfaceType in interfaces) {
            EnsureAssemblyScanned(interfaceType);
            foreach (var method in Find(interfaceType, methodName)) {
                if (TryBindExtension(interfaceType, method, out var result))
                    yield return result!;
            }
        }

        var type = forType;
        do {
            EnsureAssemblyScanned(type);
            foreach (var method in Find(type, methodName)) {
                if (TryBindExtension(type, method, out var result))
                    yield return result!;
            }

            type = type.BaseType;
        } while (type != null);
    }

    private static IEnumerable<MethodInfo> Find(Type type, string methodName) {
        var exactMatches = FindForType(type, methodName);

        if (!type.IsGenericType || !type.IsConstructedGenericType)
            return exactMatches;

        Type genDef;
        try {
            genDef = type.GetGenericTypeDefinition();
        }
        catch (InvalidOperationException) {
            return exactMatches;
        }

        var genDefMatches = FindForType(genDef, methodName);
        return exactMatches.Concat(genDefMatches);
    }

    private static IEnumerable<MethodInfo> FindForType(Type type, string methodName) {
        if (_extensions.TryGetValue(type, out var dict) && dict.TryGetValue(methodName, out var methods)) {
            return methods;
        }
        return [];
    }

    private static bool TryBindExtension(Type extensionArgType, MethodInfo method, out ExtensionCandidate? candidate) {
        candidate = null;
        var rawParams = method.GetParameters();
        if (rawParams.Length == 0) return false;

        var extParamType = rawParams[0].ParameterType;

        if (!method.IsGenericMethodDefinition) {
            // Non-generic method: owner type must be assignable to the declared extension param type.
            if (!extParamType.IsAssignableFrom(extensionArgType))
                return false;

            candidate = new ExtensionCandidate(
                method,
                new ArraySegment<ParameterInfo>(rawParams, 1, rawParams.Length - 1),
                rawParams.Skip(1).Select(p => p.ParameterType).ToArray(),
                new Dictionary<Type, Type>());
            return true;
        }

        var substitution = new Dictionary<Type, Type>();
        if (!BindGenericArgs(extParamType, extensionArgType, substitution))
            return false;

        var methodTypeArgs = method.GetGenericArguments();
        var allBound = methodTypeArgs.All(t => substitution.ContainsKey(t));

        Type[] substitutedParamTypes;
        try {
            substitutedParamTypes = rawParams.Skip(1)
                .Select(p => Substitute(p.ParameterType, substitution))
                .ToArray();
        }
        catch {
            return false;
        }

        if (allBound) {
            var concrete = methodTypeArgs.Select(t => substitution[t]).ToArray();
            MethodInfo specialized;
            try {
                specialized = method.MakeGenericMethod(concrete);
            }
            catch {
                return false;
            }

            var specRawParams = specialized.GetParameters();
            candidate = new ExtensionCandidate(
                specialized,
                new ArraySegment<ParameterInfo>(specRawParams, 1, specRawParams.Length - 1),
                specRawParams.Skip(1).Select(p => p.ParameterType).ToArray(),
                substitution);
            return true;
        }

        candidate = new ExtensionCandidate(
            method,
            new ArraySegment<ParameterInfo>(rawParams, 1, rawParams.Length - 1),
            substitutedParamTypes,
            substitution);
        return true;
    }

    internal static bool BindGenericArgs(Type paramType, Type argType, Dictionary<Type, Type> map) {
        if (paramType.IsGenericParameter) {
            if (map.TryGetValue(paramType, out var existing)) {
                return existing == argType
                    || existing.IsAssignableFrom(argType)
                    || argType.IsAssignableFrom(existing);
            }
            map[paramType] = argType;
            return true;
        }

        if (paramType.IsArray) {
            if (!argType.IsArray) return false;
            return BindGenericArgs(paramType.GetElementType()!, argType.GetElementType()!, map);
        }

        if (paramType.IsByRef) {
            var paramElem = paramType.GetElementType()!;
            var argElem = argType.IsByRef ? argType.GetElementType()! : argType;
            return BindGenericArgs(paramElem, argElem, map);
        }

        if (paramType.IsGenericType) {
            var paramDef = paramType.GetGenericTypeDefinition();
            var matched = FindGenericInstance(argType, paramDef);
            if (matched == null) return false;

            var pargs = paramType.GetGenericArguments();
            var aargs = matched.GetGenericArguments();
            if (pargs.Length != aargs.Length) return false;
            for (var i = 0; i < pargs.Length; i++) {
                if (!BindGenericArgs(pargs[i], aargs[i], map)) return false;
            }
            return true;
        }

        return paramType.IsAssignableFrom(argType);
    }

    private static Type? FindGenericInstance(Type type, Type genericDef) {
        if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == genericDef)
            return type;

        foreach (var iface in type.GetInterfaces()) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericDef)
                return iface;
        }

        var b = type.BaseType;
        while (b != null) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == genericDef)
                return b;
            b = b.BaseType;
        }
        return null;
    }

    internal static Type Substitute(Type type, Dictionary<Type, Type> map) {
        if (map.Count == 0) return type;

        if (type.IsGenericParameter) {
            return map.TryGetValue(type, out var t) ? t : type;
        }

        if (type.IsArray) {
            var elem = Substitute(type.GetElementType()!, map);
            var rank = type.GetArrayRank();
            return rank == 1 ? elem.MakeArrayType() : elem.MakeArrayType(rank);
        }

        if (type.IsByRef) {
            return Substitute(type.GetElementType()!, map).MakeByRefType();
        }

        if (type.IsGenericType && !type.IsGenericTypeDefinition) {
            var args = type.GetGenericArguments();
            var substituted = new Type[args.Length];
            var changed = false;
            for (var i = 0; i < args.Length; i++) {
                substituted[i] = Substitute(args[i], map);
                if (!ReferenceEquals(substituted[i], args[i])) changed = true;
            }
            return changed ? type.GetGenericTypeDefinition().MakeGenericType(substituted) : type;
        }

        return type;
    }
}
