using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes <see langword="false"/> as the <c>false</c> literal expression.
    /// </summary>
    public class FalseCSScriptSerializer : CSScriptSerializer
    {
        /// <summary>
        ///     The singleton instance of <see cref="FalseCSScriptSerializer"/>.
        /// </summary>
        public static readonly FalseCSScriptSerializer Instance = new FalseCSScriptSerializer();

        private FalseCSScriptSerializer()
            : base(typeof(bool))
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
    }
}