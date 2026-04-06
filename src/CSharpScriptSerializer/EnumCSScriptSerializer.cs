using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes enum values as member-access expressions (e.g., <c>MyEnum.Value</c>).
    ///     Composite flag values are represented as bitwise-OR combinations of their constituent members.
    /// </summary>
    public class EnumCSScriptSerializer : CSScriptSerializer
    {
        /// <summary>
        ///     Creates a new instance of <see cref="EnumCSScriptSerializer"/> for the given enum type.
        /// </summary>
        /// <param name="type">The enum <see cref="System.Type"/> to serialize.</param>
        public EnumCSScriptSerializer(Type type)
            : base(type)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
        {
            var name = Enum.GetName(Type, obj);
            return name == null
                ? GetCompositeValue((Enum)obj)
                : GetSimpleValue(name);
        }

        protected virtual ExpressionSyntax GetCompositeValue(Enum flags)
        {
            var simpleValues = new HashSet<Enum>(flags.GetFlags());
            foreach (var currentValue in simpleValues.ToList())
            {
                var decomposedValues = currentValue.GetFlags();
                if (decomposedValues.Count > 1)
                {
                    simpleValues.ExceptWith(decomposedValues.Where(v => !Equals(v, currentValue)));
                }
            }

            return simpleValues.Aggregate((ExpressionSyntax)null,
                (previous, current) =>
                    previous == null
                        ? GetSimpleValue(Enum.GetName(Type, current))
                        : SyntaxFactory.BinaryExpression(
                            SyntaxKind.BitwiseOrExpression, previous, GetSimpleValue(Enum.GetName(Type, current))));
        }

        protected virtual ExpressionSyntax GetSimpleValue(string name)
            => SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GetTypeSyntax(Type),
                SyntaxFactory.IdentifierName(name));
    }
}