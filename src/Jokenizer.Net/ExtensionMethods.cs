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

    internal static MethodInfo? Find(Type forType, string name, Expression?[] args) {
        var genericArgs = forType.IsConstructedGenericType ? forType.GetGenericArguments() : [];

        foreach (var extension in _extensions.Where(e => e.Name == name)) {
            var m = extension;
            if (m.IsGenericMethodDefinition) {
                if (m.GetGenericArguments().Length != genericArgs.Length) continue;

                m = m.MakeGenericMethod(genericArgs);
            }

            var allPrms = m.GetParameters();
            if (!allPrms[0].ParameterType.IsAssignableFrom(forType)) continue;

            var prms = allPrms.Skip(1).ToArray();
            if (!Helper.IsSuitable(prms, args)) continue;
                
            return m;
        }

        return null;
    }
}
