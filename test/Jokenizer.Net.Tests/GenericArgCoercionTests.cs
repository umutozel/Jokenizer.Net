using System;
using System.Collections.Generic;
using System.Linq;
using Jokenizer.Net;
using Xunit;

namespace Jokenizer.Net.Tests;

// Generic type-parameter inference used to reconcile arguments with
// assignability alone, so a candidate whose parameter was already bound by an
// earlier argument was rejected the moment a later one merely WIDENED it —
// even though the argument adapter would have converted it. Every other
// coercion site in the visitor consults Helper; this one didn't.
//
// Canonical break: `[1,2].Contains(col)` where the column is long? (runtime
// POCOs wrap value types in Nullable<>). TSource binds to int from the array
// literal, meets long?, and the whole candidate dies with "Could not find
// instance or extension method for Contains for System.Int32[]".
public class GenericArgCoercionTests {

    private class Row {
        public long? Qty { get; set; }
        public int Count { get; set; }
        public double? Price { get; set; }
        public string Region { get; set; } = "";
    }

    private static readonly IQueryable<Row> Rows = new List<Row> {
        new() { Qty = 1, Count = 1, Price = 10.5, Region = "EU" },
        new() { Qty = 2, Count = 2, Price = 20.0, Region = "US" },
        new() { Qty = 9, Count = 9, Price = 30.0, Region = "APAC" },
    }.AsQueryable();

    private static int CountWhere(string predicate) {
        var lambda = Evaluator.ToLambda<Row, bool>(predicate);
        return Rows.Count(lambda.Compile());
    }

    [Fact]
    public void IntArrayLiteral_ContainsNullableLongMember() {
        // int[] literal vs long? member — the case that was broken.
        Assert.Equal(2, CountWhere("[1,2].Contains(Qty)"));
    }

    [Fact]
    public void IntArrayLiteral_ContainsIntMember_StillExact() {
        Assert.Equal(2, CountWhere("[1,2].Contains(Count)"));
    }

    [Fact]
    public void IntArrayLiteral_ContainsNullableDoubleMember_Widens() {
        // int -> double widening plus nullable lifting in one step.
        Assert.Equal(1, CountWhere("[10.5, 99].Contains(Price)"));
    }

    [Fact]
    public void StringArrayLiteral_Unchanged() {
        // Reference types never needed the widening path — pin the regression.
        Assert.Equal(2, CountWhere("[\"EU\",\"US\"].Contains(Region)"));
    }

    [Fact]
    public void IncompatibleElementType_StillRejected() {
        // string[] vs a numeric member has no common type — must stay loud
        // rather than force a conversion.
        Assert.ThrowsAny<Exception>(() => CountWhere("[\"EU\"].Contains(Qty)"));
    }

    [Fact]
    public void MixedNumericArrayLiteral_WidensToCommonElementType() {
        // Independently broken before: the element type came from the FIRST
        // item, so [10.5, 99] threw "expression of type Int32 cannot be used to
        // initialize an array of type Double" at parse time.
        var lambda = Evaluator.ToLambda<Row, double[]>("[10.5, 99]");
        Assert.Equal(new[] { 10.5, 99d }, lambda.Compile()(new Row()));

        // Order must not matter — the narrow item may come first.
        var reversed = Evaluator.ToLambda<Row, double[]>("[99, 10.5]");
        Assert.Equal(new[] { 99d, 10.5 }, reversed.Compile()(new Row()));
    }

    [Fact]
    public void ArrayLiteralWithoutCommonType_IsRejectedNotBoxed() {
        // Falling back to object[] would make the literal compile and the
        // filter silently never match — the worst possible outcome.
        var ex = Assert.Throws<InvalidTokenException>(() => Evaluator.ToLambda<Row, object>("[\"a\", 1]"));
        Assert.Contains("no common type", ex.Message);
    }

    [Fact]
    public void CommonType_PicksTheTypeThatHoldsBoth() {
        Assert.Equal(typeof(long?), Helper.CommonType(typeof(int), typeof(long?)));
        Assert.Equal(typeof(long?), Helper.CommonType(typeof(long?), typeof(int)));
        Assert.Equal(typeof(long), Helper.CommonType(typeof(int), typeof(long)));
        Assert.Equal(typeof(double?), Helper.CommonType(typeof(int), typeof(double?)));
        Assert.Equal(typeof(int), Helper.CommonType(typeof(int), typeof(int)));
        Assert.Null(Helper.CommonType(typeof(string), typeof(int)));
        Assert.Null(Helper.CommonType(typeof(Guid), typeof(int)));
    }

    [Fact]
    public void Accepts_KeepsNullabilityHonest() {
        // A nullable source must not land in a non-nullable target: null would
        // have nowhere to go and the first null row would throw.
        Assert.False(Helper.Accepts(typeof(long), typeof(int?)));
        Assert.True(Helper.Accepts(typeof(long?), typeof(int)));
        Assert.True(Helper.Accepts(typeof(long?), typeof(int?)));
        Assert.True(Helper.Accepts(typeof(double), typeof(int)));
        Assert.False(Helper.Accepts(typeof(int), typeof(long)));   // narrowing is not implicit
        Assert.True(Helper.Accepts(typeof(object), typeof(string)));
    }
}
