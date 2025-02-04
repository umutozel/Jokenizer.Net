using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Jokenizer.Net {

    public static class Helper {
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

            (typeof(float), typeof(double))
        ];

        public static bool IsSuitable(ParameterInfo[] prms, Expression?[] args) {
            if (prms.Length != args.Length) return false;

            for (var i = 0; i < prms.Length; i++) {
                var prm = prms[i];
                var arg = args[i];

                if (arg == null) {
                    if (!typeof(Delegate).IsAssignableFrom(prm.ParameterType))
                        return false;
                }
                else if (!CanConvert(prm.ParameterType, arg.Type))
                    return false;
            }

            return true;
        }

        private static bool CanConvert(Type from, Type to) {
            if (from == to)
                return true;

            var nonNullableFrom = Nullable.GetUnderlyingType(from) ?? from;
            var nonNullableTo = Nullable.GetUnderlyingType(to) ?? to;

            return IsImplicitlyConvertible(nonNullableFrom, nonNullableTo);
        }

        private static bool IsImplicitlyConvertible(Type from, Type to) => _implicitNumericConversions.Contains((from, to));
    }
}
