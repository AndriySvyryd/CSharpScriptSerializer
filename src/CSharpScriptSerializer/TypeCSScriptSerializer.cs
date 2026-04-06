using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes <see cref="System.Type"/> values as <c>typeof()</c> expressions.
    /// </summary>
    public class TypeCSScriptSerializer : CSScriptSerializer
    {
        /// <summary>
        ///     Creates a new instance of <see cref="TypeCSScriptSerializer"/> for the given type.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> being serialized (the type whose instances are <see cref="System.Type"/> objects).</param>
        public TypeCSScriptSerializer(Type type)
            : base(type)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.TypeOfExpression(GetTypeSyntax((Type) obj));
    }
}