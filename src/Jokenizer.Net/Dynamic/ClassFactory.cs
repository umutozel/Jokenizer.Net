using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Jokenizer.Net.Dynamic;

internal class ClassFactory {
    private static readonly object _classesLocker = new();
    public static readonly ClassFactory Instance = new();
    private readonly ModuleBuilder _module;
    private readonly Dictionary<Signature, Type> _classes;

    static ClassFactory() { }  // Trigger lazy initialization of static fields

    private ClassFactory() {
        var name = new AssemblyName("DynamicClasses");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        _module = assembly.DefineDynamicModule("Module");
        _classes = new Dictionary<Signature, Type>();
    }

    public Type GetDynamicClass(IEnumerable<DynamicProperty> properties) {
        var signature = new Signature(properties);
        if (_classes.TryGetValue(signature, out var @class))
            return @class;

        lock (_classesLocker) {
            if (!_classes.ContainsKey(signature))
                _classes.Add(signature, CreateDynamicClass(signature.Properties));
        }

        return _classes[signature];
    }

    private Type CreateDynamicClass(DynamicProperty[] properties) {
        var typeName = "DynamicClass" + (_classes.Count + 1);
        var tb = _module.DefineType(
            typeName,
            TypeAttributes.Class | TypeAttributes.Public, typeof(DynamicClass)
        );

        var fields = GenerateProperties(tb, properties);
        GenerateEquals(tb, fields);
        GenerateGetHashCode(tb, fields);
        return tb.CreateTypeInfo()!.AsType();
    }

    private static FieldBuilder[] GenerateProperties(TypeBuilder tb, DynamicProperty[] properties) {
        var fields = new FieldBuilder[properties.Length];
        for (var i = 0; i < properties.Length; i++) {
            var dp = properties[i];
            var fb = tb.DefineField("_" + dp.Name, dp.Type, FieldAttributes.Private);
            var pb = tb.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);
            var mbGet = tb.DefineMethod("get_" + dp.Name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                dp.Type, Type.EmptyTypes);
            var genGet = mbGet.GetILGenerator();
            genGet.Emit(OpCodes.Ldarg_0);
            genGet.Emit(OpCodes.Ldfld, fb);
            genGet.Emit(OpCodes.Ret);
            pb.SetGetMethod(mbGet);

            var mbSet = tb.DefineMethod("set_" + dp.Name,
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                null, [dp.Type]);
            mbSet.DefineParameter(1, ParameterAttributes.In, "_" + dp.Name);
            var genSet = mbSet.GetILGenerator();
            genSet.Emit(OpCodes.Ldarg_0);
            genSet.Emit(OpCodes.Ldarg_1);
            genSet.Emit(OpCodes.Stfld, fb);
            genSet.Emit(OpCodes.Ret);
            pb.SetSetMethod(mbSet);

            fields[i] = fb;
        }

        return fields;
    }

    private static void GenerateEquals(TypeBuilder tb, FieldBuilder[] fields) {
        var mb = tb.DefineMethod("Equals",
            MethodAttributes.Public | MethodAttributes.ReuseSlot |
            MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(bool), [typeof(object)]
        );
        var gen = mb.GetILGenerator();
        var other = gen.DeclareLocal(tb);
        var next = gen.DefineLabel();
        gen.Emit(OpCodes.Ldarg_1);
        gen.Emit(OpCodes.Isinst, tb);
        gen.Emit(OpCodes.Stloc, other);
        gen.Emit(OpCodes.Ldloc, other);
        gen.Emit(OpCodes.Brtrue_S, next);
        gen.Emit(OpCodes.Ldc_I4_0);
        gen.Emit(OpCodes.Ret);
        gen.MarkLabel(next);

        foreach (var field in fields) {
            var ft = field.FieldType;
            var ct = typeof(EqualityComparer<>).MakeGenericType(ft);
            next = gen.DefineLabel();
            gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default")!, null);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.Emit(OpCodes.Ldloc, other);
            gen.Emit(OpCodes.Ldfld, field);
            gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("Equals", [ft, ft])!, null);
            gen.Emit(OpCodes.Brtrue_S, next);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);
            gen.MarkLabel(next);
        }

        gen.Emit(OpCodes.Ldc_I4_1);
        gen.Emit(OpCodes.Ret);
    }

    private static void GenerateGetHashCode(TypeBuilder tb, FieldBuilder[] fields) {
        var mb = tb.DefineMethod("GetHashCode",
            MethodAttributes.Public | MethodAttributes.ReuseSlot |
            MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(int), Type.EmptyTypes);
        var gen = mb.GetILGenerator();
        gen.Emit(OpCodes.Ldc_I4_0);

        foreach (var field in fields) {
            var ft = field.FieldType;
            var ct = typeof(EqualityComparer<>).MakeGenericType(ft);
            gen.EmitCall(OpCodes.Call, ct.GetMethod("get_Default")!, null);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
            gen.EmitCall(OpCodes.Callvirt, ct.GetMethod("GetHashCode", [ft])!, null);
            gen.Emit(OpCodes.Xor);
        }

        gen.Emit(OpCodes.Ret);
    }
}
