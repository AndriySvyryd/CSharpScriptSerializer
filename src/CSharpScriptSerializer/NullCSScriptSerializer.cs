using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes <see langword="null"/> as the <c>null</c> literal expression.
    /// </summary>
    public class NullCSScriptSerializer : CSScriptSerializer
    {
        /// <summary>
        ///     The singleton instance of <see cref="NullCSScriptSerializer"/>.
        /// </summary>
        public static readonly NullCSScriptSerializer Instance = new NullCSScriptSerializer();

        private NullCSScriptSerializer()
            : base(type: null)
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
    }
}