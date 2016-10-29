using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    public class NullCSScriptSerializer : CSScriptSerializer
    {
        public static readonly NullCSScriptSerializer Instance = new NullCSScriptSerializer();

        private NullCSScriptSerializer()
            : base(type: null)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
    }
}