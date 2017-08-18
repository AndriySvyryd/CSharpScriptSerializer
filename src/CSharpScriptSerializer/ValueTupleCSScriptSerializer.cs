using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpScriptSerialization
{
    public class ValueTupleCSScriptSerializer : CSScriptSerializer
    {
        private readonly IReadOnlyCollection<Func<object, object>> _constructorParameterGetters;

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