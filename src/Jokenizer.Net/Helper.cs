using System;
using System.Collections.Generic;

namespace Jokenizer.Net;

public static class Helper {

    public static bool CanConvert(Type to, Type from) {
        if (to.IsEnum) {
            to = Enum.GetUnderlyingType(to);
        }

        if (from == to || from.IsAssignableFrom(to))
            return true;

        var nonNullableFrom = Nullable.GetUnderlyingType(from) ?? from;
        var nonNullableTo = Nullable.GetUnderlyingType(to) ?? to;

        return IsImplicitlyConvertible(nonNullableFrom, nonNullableTo);
    }

    /// <summary>
    /// Can a value of <paramref name="source"/> be used where
    /// <paramref name="target"/> is expected? Unambiguously named counterpart of
    /// <see cref="CanConvert"/> (whose parameter order reads differently at its
    /// two call sites): the direction is always target ← source.
    /// <para>A nullable source needs a nullable target — otherwise null has
    /// nowhere to go, and silently dropping that is how a filter starts
    /// throwing on the first null row.</para>
    /// </summary>
    public static bool Accepts(Type target, Type source) {
        if (target == source) return true;
        if (target.IsAssignableFrom(source)) return true;

        var targetUnderlying = Nullable.GetUnderlyingType(target);
        var sourceUnderlying = Nullable.GetUnderlyingType(source);
        if (sourceUnderlying != null && targetUnderlying == null) return false;

        var from = sourceUnderlying ?? source;
        var to = targetUnderlying ?? target;
        if (to.IsEnum) to = Enum.GetUnderlyingType(to);

        return from == to || IsImplicitlyConvertible(from, to);
    }

    /// <summary>
    /// The narrowest type that can hold values of BOTH inputs, or null when
    /// there isn't one. Used for generic type-parameter inference when one
    /// argument binds the parameter and a later one is wider — e.g.
    /// <c>[1,2].Contains(longColumn)</c> binds TSource from the array literal
    /// as <c>int</c>, then meets <c>long?</c>; the common type is <c>long?</c>.
    /// </summary>
    public static Type? CommonType(Type a, Type b) {
        if (a == b) return a;
        if (Accepts(a, b)) return a;
        if (Accepts(b, a)) return b;

        // Neither side accepts the other as-is: this is the mixed-nullability
        // case (int vs long?). Widen AND lift, then verify both fit.
        var an = Nullable.GetUnderlyingType(a) ?? a;
        var bn = Nullable.GetUnderlyingType(b) ?? b;
        if (!an.IsValueType || !bn.IsValueType) return null;

        Type? wider = IsImplicitlyConvertible(an, bn) ? bn
                    : IsImplicitlyConvertible(bn, an) ? an
                    : null;
        if (wider == null) return null;

        var lifted = typeof(Nullable<>).MakeGenericType(wider);
        return Accepts(lifted, a) && Accepts(lifted, b) ? lifted : null;
    }

    private static bool IsImplicitlyConvertible(Type from, Type to) => _implicitNumericConversions.Contains((from, to));

    private static readonly HashSet<(Type, Type)> _implicitNumericConversions = [
        (typeof(sbyte), typeof(short)), (typeof(sbyte), typeof(int)), (typeof(sbyte), typeof(long)),
        (typeof(sbyte), typeof(float)), (typeof(sbyte), typeof(double)), (typeof(sbyte), typeof(decimal)),

        (typeof(byte), typeof(short)), (typeof(byte), typeof(ushort)), (typeof(byte), typeof(int)),
        (typeof(byte), typeof(uint)), (typeof(byte), typeof(long)), (typeof(byte), typeof(ulong)),
        (typeof(byte), typeof(float)), (typeof(byte), typeof(double)), (typeof(byte), typeof(decimal)),

        (typeof(short), typeof(int)), (typeof(short), typeof(long)), (typeof(short), typeof(float)),
        (typeof(short), typeof(double)), (typeof(short), typeof(decimal)),

        (typeof(ushort), typeof(int)), (typeof(ushort), typeof(uint)), (typeof(ushort), typeof(long)),
        (typeof(ushort), typeof(ulong)), (typeof(ushort), typeof(float)), (typeof(ushort), typeof(double)),
        (typeof(ushort), typeof(decimal)),

        (typeof(int), typeof(long)), (typeof(int), typeof(float)), (typeof(int), typeof(double)),
        (typeof(int), typeof(decimal)),

        (typeof(uint), typeof(long)), (typeof(uint), typeof(ulong)), (typeof(uint), typeof(float)),
        (typeof(uint), typeof(double)), (typeof(uint), typeof(decimal)),

        (typeof(long), typeof(float)), (typeof(long), typeof(double)), (typeof(long), typeof(decimal)),

        (typeof(ulong), typeof(float)), (typeof(ulong), typeof(double)), (typeof(ulong), typeof(decimal)),

        (typeof(float), typeof(double)),

        (typeof(char), typeof(int)), (typeof(char), typeof(long)), (typeof(char), typeof(float)),
        (typeof(char), typeof(double)), (typeof(char), typeof(decimal))
    ];
}
