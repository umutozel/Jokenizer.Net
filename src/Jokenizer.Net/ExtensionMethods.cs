using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jokenizer.Net;

public static class ExtensionMethods {
    private static readonly HashSet<MethodInfo> _extensions = [];

    static ExtensionMethods() => ScanTypes(typeof(Queryable), typeof(Enumerable));

    public static IList<MethodInfo> ProbeAllAssemblies() =>
        ProbeAssemblies(Assembly.GetEntryAssembly()!.GetReferencedAssemblies().Select(Assembly.Load));
    public static IList<MethodInfo> ProbeAssemblies(params Assembly[] assemblies) =>
        assemblies.SelectMany(ProbeAssembly).ToList();
    public static IList<MethodInfo> ProbeAssemblies(IEnumerable<Assembly> assemblies) =>
        assemblies.SelectMany(ProbeAssembly).ToList();
    public static IList<MethodInfo> ProbeAssembly(Assembly assembly) =>
        ScanTypes(assembly.GetTypes().Where(t => t.IsSealed && t is { IsGenericType: false, IsNested: false }));
    public static IList<MethodInfo> ScanTypes(params Type[] types) => types.SelectMany(ScanType).ToList();
    public static IList<MethodInfo> ScanTypes(IEnumerable<Type> types) => types.SelectMany(ScanType).ToList();

    public static IList<MethodInfo> ScanType(Type type) {
        var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                          .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
                          .ToList();

        methods.ForEach(m => _extensions.Add(m));

        return methods;
    }

    public static MethodInfo? Find(Type forType, string name, Expression[] availableArgs) {
        var args = forType.IsConstructedGenericType ? forType.GetGenericArguments() : [];

        foreach (var extension in _extensions.Where(e => e.Name == name)) {
            var m = extension;
            if (m.IsGenericMethodDefinition) {
                if (m.GetGenericArguments().Length != args.Length) continue;

                m = m.MakeGenericMethod(args);
            }

            var prms = m.GetParameters();
            if (!prms[0].ParameterType.IsAssignableFrom(forType)) continue;
            if (!Helper.IsSuitable(prms.Skip(1).ToArray(), availableArgs)) continue;
                
            return m;
        }

        return null;
    }
}
