using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting.Hosting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace CSharpScriptSerialization
{
    public class LiteralCSScriptSerializer : CSScriptSerializer
    {
        public LiteralCSScriptSerializer(Type type, SyntaxKind kind)
            : base(type)
        {
            Kind = kind;
        }

        protected SyntaxKind Kind { get; }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.LiteralExpression(
                Kind,
                LiteralFactories[obj.GetType()](obj));

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
    }
}