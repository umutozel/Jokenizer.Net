using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Jokenizer.Net;

using Tokens;

public class Tokenizer {
    private static readonly string _separator = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

    protected readonly Settings Settings;
    protected readonly string Exp;
    private readonly int _len;
    protected int Idx { get; private set; }
    protected char Ch { get; private set; }

    public Tokenizer(string exp, Settings? settings = null) {
        if (exp == null)
            throw new ArgumentNullException(nameof(exp));
        if (string.IsNullOrWhiteSpace(exp))
            throw new ArgumentException(nameof(exp));

        Settings = settings ?? Settings.Default;
        Exp = exp;

        _len = exp.Length;
        Ch = exp.ElementAt(0);
    }

    protected virtual Token? GetToken() {
        Skip();

        var t = TryLiteral()
                ?? TryVariable()
                ?? TryParameter()
                ?? TryUnary()
                ?? TryGroup()
                ?? TryObject()
                ?? (Token?)TryShortArray();

        switch (t) {
            case null:
                return null;
            case VariableToken vt when Settings.TryGetKnownValue(vt.Name, out var value):
                t = new LiteralToken(value);
                break;
            case VariableToken vt: {
                if (vt.Name == "new") {
                    Skip();
                    t = (Token?)TryObject() ?? GetArray();
                }
                break;
            }
        }

        if (Done()) return t;

        Token r;
        do {
            Skip();

            r = t;
            t = TryMember(t)
                ?? TryIndexer(t)
                ?? TryLambda(t)
                ?? TryCall(t)
                ?? TryTernary(t)
                ?? (Token?)TryBinary(t);
        } while (t != null);

        return r;
    }

    protected virtual LiteralToken? TryNumber() {
        var n = GetNumber();
        if (n == "") return null;

        var isFloat = false;
        if (Get(_separator)) {
            n += _separator;
            n += GetNumber();
            isFloat = true;
        }

        if (IsVariableStart())
            throw new InvalidSyntaxException($"Unexpected character (${Ch}) at index ${Idx}");

        var val = isFloat ? float.Parse(n) : Convert.ChangeType(int.Parse(n), typeof(int));
        return new LiteralToken(val);
    }

    protected string GetNumber() {
        var n = "";
        while (IsNumber()) {
            n += Ch;
            Move();
        }

        return n;
    }

    protected virtual Token? TryString() {
        var inter = false;
        if (Ch == '$') {
            inter = true;
            Move();
        }

        if (Ch != '"') return null;

        var q = Ch;
        var es = new List<Token>();
        var s = "";

        for (var c = Move(); !Done(); c = Move()) {
            if (c == q) {
                Move();

                if (es.Count <= 0) return new LiteralToken(s);

                if (s != "") {
                    es.Add(new LiteralToken(s));
                }

                return es.Aggregate(
                    (Token)new LiteralToken(""),
                    (p, n) => new BinaryToken("+", p, n)
                );
            }

            if (c == '\\') {
                c = Move();
                s += c switch {
                    'a'  => '\a',
                    'b'  => '\b',
                    'f'  => '\f',
                    'n'  => '\n',
                    'r'  => '\r',
                    't'  => '\t',
                    'v'  => '\v',
                    '0'  => '\0',
                    '\\' => '\\',
                    '"'  => '"',
                    _    => "\\" + c
                };
            }
            else if (inter && Get("{")) {
                if (s != "") {
                    es.Add(new LiteralToken(s));
                    s = "";
                }

                var interExp = GetToken();
                if (interExp is null) throw new InvalidSyntaxException($"Invalid interpolation at index ${Idx}");
                es.Add(interExp);

                Skip();
                if (Ch != '}')
                    throw new InvalidSyntaxException($"Unterminated template literal at {Idx}");
            }
            else {
                s += c;
            }
        }

        throw new InvalidSyntaxException($"Unclosed quote after {s}");
    }

    protected virtual Token? TryLiteral() {
        return TryNumber() ?? TryString();
    }

    protected virtual VariableToken? TryVariable() {
        var v = GetVariableName();
        return v != "" ? new VariableToken(v) : null;
    }

    protected string GetVariableName() {
        var v = "";

        if (IsVariableStart()) {
            do {
                v += Ch;
                Move();
            } while (StillVariable());
        }

        return v;
    }

    protected virtual VariableToken? TryParameter() {
        if (!Get("@")) return null;

        var n = GetNumber();
        if (string.IsNullOrEmpty(n))
            throw new InvalidSyntaxException($"Invalid parameter at {Idx}");

        return new VariableToken("@" + n);
    }

    protected virtual UnaryToken? TryUnary() {
        if (!Settings.ContainsUnary(Ch)) return null;

        var u = Ch;
        Move();
        var exp = GetToken();
        if (exp is null) throw new InvalidSyntaxException($"Invalid unary expression at {Idx}");

        return new UnaryToken(u, exp);
    }

    protected virtual GroupToken? TryGroup() => Get("(") ? new GroupToken(GetGroup()) : null;

    protected virtual IEnumerable<Token> GetGroup() {
        var es = new List<Token>();
        do {
            var e = GetToken();
            if (e != null) {
                es.Add(e);
            }
        } while (Get(","));

        To(")");

        return es;
    }

    protected virtual ObjectToken? TryObject() {
        if (!Get("{")) return null;

        var es = new List<AssignToken>();
        do {
            Skip();
            var member = GetToken();
            if (member is not IVariableToken vt)
                throw new InvalidSyntaxException($"Invalid assignment at {Idx}");

            Skip();
            if (Get("=")) {
                if (member.GetType() != typeof(VariableToken))
                    throw new InvalidSyntaxException($"Invalid assignment at {Idx}");

                Skip();

                var exp = GetToken();
                if (exp is null) throw new InvalidSyntaxException($"Invalid assignment at {Idx}");

                es.Add(new AssignToken(vt.Name, exp));
            }
            else {
                es.Add(new AssignToken(vt.Name, member));
            }
        } while (Get(","));

        To("}");

        return new ObjectToken(es);
    }

    protected virtual ArrayToken? TryShortArray() {
        if (!Get("[")) return null;

        var tokens = GetArrayTokens();
        To("]");

        return new ArrayToken(tokens);
    }

    protected virtual ArrayToken GetArray() {
        To("[");
        To("]");
        To("{");
        var tokens = GetArrayTokens();
        To("}");

        return new ArrayToken(tokens);
    }

    protected IEnumerable<Token> GetArrayTokens() {
        var ts = new List<Token>();
        do {
            Skip();
            var token = GetToken();
            if (token == null) {
                if (ts.Count > 0)
                    throw new InvalidSyntaxException($"Invalid array item at {Idx}");

                break;
            }

            ts.Add(token);
        } while (Get(","));

        return ts;
    }

    protected virtual MemberToken? TryMember(Token t) {
        if (!Get(".")) return null;

        Skip();
        var v = GetVariableName();
        if (string.IsNullOrEmpty(v)) throw new InvalidSyntaxException($"Invalid member identifier at {Idx}");

        return new MemberToken(t, v);
    }

    protected virtual IndexerToken? TryIndexer(Token t) {
        if (!Get("[")) return null;

        Skip();

        var k = GetToken();
        if (k is null) throw new InvalidSyntaxException($"Invalid indexer at {Idx}");

        To("]");

        return new IndexerToken(t, k);
    }

    protected virtual LambdaToken? TryLambda(Token t) {
        if (!Get("=>"))
            return null;

        var exp = GetToken();
        if (exp is null) throw new InvalidSyntaxException($"Invalid lambda at {Idx}");

        return new LambdaToken(exp, GetParameters(t));
    }

    protected IEnumerable<string> GetParameters(Token t) {
        if (t is GroupToken gt) {
            return gt.Tokens.Select(x => {
                if (x is not IVariableToken xv)
                    throw new InvalidSyntaxException($"Invalid parameter at {Idx}");

                return xv.Name;
            });
        }

        if (t is not IVariableToken vt)
            throw new InvalidSyntaxException($"Invalid parameter at {Idx}");

        return [vt.Name];
    }

    protected virtual CallToken? TryCall(Token t) {
        return Get("(") ? GetCall(t) : null;
    }

    protected CallToken GetCall(Token t) {
        var args = GetGroup();

        return new CallToken(t, args);
    }

    protected virtual TernaryToken? TryTernary(Token t) {
        if (!Get("?")) return null;

        var whenTrue = GetToken();
        if (whenTrue is null) throw new InvalidSyntaxException($"Invalid ternary at {Idx}");

        To(":");
        var whenFalse = GetToken();
        if (whenFalse is null) throw new InvalidSyntaxException($"Invalid ternary at {Idx}");

        return new TernaryToken(t, whenTrue, whenFalse);
    }

    protected virtual BinaryToken? TryBinary(Token t) {
        var op = Settings.BinaryOperators.FirstOrDefault(Get);
        if (op == null) return null;

        var right = GetToken();
        return right switch {
            null           => throw new InvalidSyntaxException($"Invalid binary at {Idx}"),
            BinaryToken bt => FixPrecedence(t, op, bt),
            _              => new BinaryToken(op, t, right)
        };
    }

    protected bool IsSpace() => char.IsWhiteSpace(Ch);

    protected bool IsNumber() => char.IsNumber(Ch);

    protected bool IsVariableStart() =>
        Ch == 95                    // `_`
        || (Ch >= 65 && Ch <= 90)   // A...Z
        || (Ch >= 97 && Ch <= 122); // a...z

    protected bool StillVariable() => IsVariableStart() || IsNumber();

    protected bool Done() => Idx >= _len;

    protected char Move(int count = 1) {
        Idx += count;
        var d = Done();
        return Ch = d ? '\0' : Exp.ElementAt(Idx);
    }

    protected bool Get(string s) {
        if (!Eq(Idx, s)) return false;

        Move(s.Length);
        return true;
    }

    protected void Skip() {
        while (IsSpace()) Move();
    }

    protected bool Eq(int index, string target) {
        if (index + target.Length > Exp.Length) return false;
        return Exp.Substring(Idx, target.Length) == target;
    }

    protected void To(string c) {
        Skip();

        if (!Eq(Idx, c))
            throw new InvalidSyntaxException($"Expected {c} at index {Idx}, found {Ch}");

        Move(c.Length);
    }

    protected BinaryToken FixPrecedence(Token left, string leftOp, BinaryToken right) {
        Settings.TryGetBinaryInfo(leftOp, out var lo);
        Settings.TryGetBinaryInfo(right.Operator, out var ro);

        return ro.Precedence < lo.Precedence
            ? new BinaryToken(right.Operator, new BinaryToken(leftOp, left, right.Left), right.Right)
            : new BinaryToken(leftOp, left, right);
    }

    public static Token? Parse(string exp, Settings? settings = null) => new Tokenizer(exp, settings).GetToken();

    public static T? Parse<T>(string exp, Settings? settings = null) where T : Token => (T?)Parse(exp, settings);
}
