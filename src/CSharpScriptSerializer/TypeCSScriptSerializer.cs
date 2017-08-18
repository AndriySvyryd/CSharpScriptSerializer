using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    public class TypeCSScriptSerializer : CSScriptSerializer
    {
        public TypeCSScriptSerializer(Type type)
            : base(type)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.TypeOfExpression(GetTypeSyntax((Type) obj));
    }
}