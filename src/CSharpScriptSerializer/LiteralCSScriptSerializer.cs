using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes primitive values (<see cref="int"/>, <see cref="string"/>, <see cref="char"/>, etc.)
    ///     as C# literal expressions.
    /// </summary>
    public class LiteralCSScriptSerializer : CSScriptSerializer
    {
        private static readonly Dictionary<Type, Func<object, SyntaxToken>> LiteralFactories =
            new Dictionary<Type, Func<object, SyntaxToken>>
            {
                {typeof(char), x => SyntaxFactory.Literal((char)x)},
                {typeof(decimal), x => SyntaxFactory.Literal((decimal)x)},
                {typeof(double), x => SyntaxFactory.Literal((double)x)},
                {typeof(float), x => SyntaxFactory.Literal((float)x)},
                {typeof(int), x => SyntaxFactory.Literal((int)x)},
                {typeof(long), x => SyntaxFactory.Literal((long)x)},
                {
                    typeof(string), x => SyntaxFactory.Literal(
                        SyntaxTriviaList.Empty,
                        CSharpObjectFormatter.Instance.FormatObject(x,
                            new PrintOptions {EscapeNonPrintableCharacters = false, MaximumOutputLength = int.MaxValue}),
                        (string)x,
                        SyntaxTriviaList.Empty)
                },
                {typeof(uint), x => SyntaxFactory.Literal((uint)x)},
                {typeof(ulong), x => SyntaxFactory.Literal((ulong)x)},
                {typeof(short), x => SyntaxFactory.Literal((short)x)},
                {typeof(byte), x => SyntaxFactory.Literal((byte)x)},
                {typeof(ushort), x => SyntaxFactory.Literal((ushort)x)},
                {typeof(sbyte), x => SyntaxFactory.Literal((sbyte)x)}
            };

        /// <summary>
        ///     Creates a new instance of <see cref="LiteralCSScriptSerializer"/> for the given type and literal kind.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the literal value.</param>
        /// <param name="kind">The Roslyn <see cref="SyntaxKind"/> of the literal expression (e.g., <see cref="SyntaxKind.NumericLiteralExpression"/>).</param>
        public LiteralCSScriptSerializer(Type type, SyntaxKind kind)
            : base(type)
            => Kind = kind;

        /// <summary>
        ///     Gets the Roslyn <see cref="SyntaxKind"/> used for the literal expression.
        /// </summary>
        protected SyntaxKind Kind { get; }

        public override ExpressionSyntax GetCreation(object obj) => SyntaxFactory.LiteralExpression(
            Kind,
            LiteralFactories[obj.GetType()](obj));
    }
}