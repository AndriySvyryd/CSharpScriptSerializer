using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes value-tuple types as C# tuple expressions (e.g., <c>(1, "hello")</c>).
    /// </summary>
    public class ValueTupleCSScriptSerializer : CSScriptSerializer
    {
        private readonly IReadOnlyCollection<Func<object, object>> _constructorParameterGetters;

        /// <summary>
        ///     Creates a new instance of <see cref="ValueTupleCSScriptSerializer"/> for the given value-tuple type.
        /// </summary>
        /// <param name="type">The value-tuple <see cref="System.Type"/> to serialize.</param>
        /// <param name="constructorParameterGetters">
        ///     Getters for each tuple element in declaration order.
        /// </param>
        public ValueTupleCSScriptSerializer(Type type,
            IReadOnlyCollection<Func<object, object>> constructorParameterGetters)
            : base(type) => _constructorParameterGetters = constructorParameterGetters;

        public override ExpressionSyntax GetCreation(object obj)
        {
            return TupleExpression(SeparatedList<ArgumentSyntax>(
                ToCommaSeparatedList(
                    _constructorParameterGetters.Select(getParameter =>
                        Argument(GetCreationExpression(getParameter(obj)))))));
        }
    }
}