using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpScriptSerialization
{
    public class ArrayCSScriptSerializer : CSScriptSerializer
    {
        public ArrayCSScriptSerializer(Type type)
            : base(type)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
        {
            var array = (Array)obj;
            return array.Length == 0
                ? ArrayCreationExpression(ArrayType(GetTypeSyntax(GetArrayElementType(Type)),
                    List(GetEmptyArrayRanks(Type))))
                : ArrayCreationExpression((ArrayTypeSyntax)GetTypeSyntax(Type)).WithInitializer(AddNewLine(
                    GetArrayInitializerExpression(array, startingDimension: 0, indices: ImmutableArray<int>.Empty)));
        }

        private static IEnumerable<ArrayRankSpecifierSyntax> GetEmptyArrayRanks(Type type)
            => new[]
                {
                    ArrayRankSpecifier(
                        SeparatedList<ExpressionSyntax>(
                            ToCommaSeparatedList(                            
                                Enumerable.Repeat(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                                    type.GetArrayRank()))))
                }
                .Concat(GetArrayRanks(type.GetElementType()));

        private InitializerExpressionSyntax GetArrayInitializerExpression
            (Array array, int startingDimension, ImmutableArray<int> indices)
            => InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SeparatedList<ExpressionSyntax>(
                    ToCommaSeparatedList(Enumerable.Range(
                        array.GetLowerBound(startingDimension),
                        array.GetUpperBound(startingDimension) - array.GetLowerBound(startingDimension) + 1)
                        .Select(i => array.Rank > startingDimension + 1
                            ? GetArrayInitializerExpression(array, startingDimension + 1, indices.Add(i))
                            : GetCreationExpression(array.GetValue(indices.Add(i).ToArray()))))));
    }
}