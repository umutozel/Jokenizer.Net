using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Jokenizer.Net {
    using Tokens;

    public static class Evaluator {

        #region Lambda

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                                Settings settings, params object[] parameters) {
            return new TokenVisitor(variables, parameters, settings).Process(token, typeParameters);
        }

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                                params object[] parameters) {
            return ToLambda(token, typeParameters, variables, null, parameters);
        }

        public static LambdaExpression ToLambda(Token token, IEnumerable<Type> typeParameters, params object[] parameters) {
            return ToLambda(token, typeParameters, null, parameters);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, IDictionary<string, object> variables,
                                                                    params object[] parameters) {
            return (Expression<Func<TResult>>)ToLambda(token, null, variables, parameters);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(Token token, params object[] parameters) {
            return ToLambda<TResult>(token, null, parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, IDictionary<string, object> variables,
                                                                        params object[] parameters) {
            return (Expression<Func<T, TResult>>)ToLambda(token, new[] { typeof(T) }, variables, parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(Token token, params object[] parameters) {
            return ToLambda<T, TResult>(token, null, parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, IDictionary<string, object> variables,
                                                                                    params object[] parameters) {
            return (Expression<Func<T1, T2, TResult>>)ToLambda(token, new[] { typeof(T1), typeof(T2) }, variables, parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(Token token, params object[] parameters) {
            return ToLambda<T1, T2, TResult>(token, null, parameters);
        }

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                                Settings settings, params object[] parameters) {
            return ToLambda(Tokenizer.Parse(token, settings), typeParameters, variables, parameters);
        }

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                                params object[] parameters) {
            return ToLambda(token, typeParameters, variables, null, parameters);
        }

        public static LambdaExpression ToLambda(string token, IEnumerable<Type> typeParameters, params object[] parameters) {
            return ToLambda(Tokenizer.Parse(token), typeParameters, parameters);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, IDictionary<string, object> variables,
                                                                    params object[] parameters) {
            return ToLambda<TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Expression<Func<TResult>> ToLambda<TResult>(string token, params object[] parameters) {
            return ToLambda<TResult>(Tokenizer.Parse(token), parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, IDictionary<string, object> variables,
                                                                        params object[] parameters) {
            return ToLambda<T, TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Expression<Func<T, TResult>> ToLambda<T, TResult>(string token, params object[] parameters) {
            return ToLambda<T, TResult>(Tokenizer.Parse(token), parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, IDictionary<string, object> variables,
                                                                                    params object[] parameters) {
            return ToLambda<T1, T2, TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Expression<Func<T1, T2, TResult>> ToLambda<T1, T2, TResult>(string token, params object[] parameters) {
            return ToLambda<T1, T2, TResult>(Tokenizer.Parse(token), parameters);
        }

        #endregion

        #region Func

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                        Settings settings, params object[] parameters) {
            return ToLambda(token, typeParameters, variables, settings, parameters).Compile();
        }

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                        params object[] parameters) {
            return ToFunc(token, typeParameters, variables, null, parameters);
        }

        public static Delegate ToFunc(Token token, IEnumerable<Type> typeParameters, params object[] parameters) {
            return ToFunc(token, typeParameters, null, parameters);
        }

        public static Func<TResult> ToFunc<TResult>(Token token, IDictionary<string, object> variables,
                                                    params object[] parameters) {
            return (Func<TResult>)ToFunc(token, null, variables, parameters);
        }

        public static Func<TResult> ToFunc<TResult>(Token token, params object[] parameters) {
            return ToFunc<TResult>(token, null, parameters);
        }

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, IDictionary<string, object> variables,
                                                            params object[] parameters) {
            return (Func<T, TResult>)ToFunc(token, new[] { typeof(T) }, variables, parameters);
        }

        public static Func<T, TResult> ToFunc<T, TResult>(Token token, params object[] parameters) {
            return ToFunc<T, TResult>(token, null, parameters);
        }

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, IDictionary<string, object> variables,
                                                                    params object[] parameters) {
            return (Func<T1, T2, TResult>)ToFunc(token, new[] { typeof(T1), typeof(T2) }, variables, parameters);
        }

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(Token token, params object[] parameters) {
            return ToFunc<T1, T2, TResult>(token, null, parameters);
        }

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, IDictionary<string, object> variables,
                                        params object[] parameters) {
            return ToFunc(Tokenizer.Parse(token), typeParameters, variables, parameters);
        }

        public static Delegate ToFunc(string token, IEnumerable<Type> typeParameters, params object[] parameters) {
            return ToFunc(Tokenizer.Parse(token), typeParameters, parameters);
        }

        public static Func<TResult> ToFunc<TResult>(string token, IDictionary<string, object> variables,
                                                    params object[] parameters) {
            return ToFunc<TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Func<TResult> ToFunc<TResult>(string token, params object[] parameters) {
            return ToFunc<TResult>(Tokenizer.Parse(token), parameters);
        }

        public static Func<T, TResult> ToFunc<T, TResult>(string token, IDictionary<string, object> variables,
                                                            params object[] parameters) {
            return ToFunc<T, TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Func<T, TResult> ToFunc<T, TResult>(string token, params object[] parameters) {
            return ToFunc<T, TResult>(Tokenizer.Parse(token), parameters);
        }

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, IDictionary<string, object> variables,
                                                                    params object[] parameters) {
            return ToFunc<T1, T2, TResult>(Tokenizer.Parse(token), variables, parameters);
        }

        public static Func<T1, T2, TResult> ToFunc<T1, T2, TResult>(string token, params object[] parameters) {
            return ToFunc<T1, T2, TResult>(Tokenizer.Parse(token), parameters);
        }

        #endregion
    }
}
