using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes <see langword="true"/> as the <c>true</c> literal expression.
    /// </summary>
    public class TrueCSScriptSerializer : CSScriptSerializer
    {
        /// <summary>
        ///     The singleton instance of <see cref="TrueCSScriptSerializer"/>.
        /// </summary>
        public static readonly TrueCSScriptSerializer Instance = new TrueCSScriptSerializer();

        private TrueCSScriptSerializer()
            : base(typeof(bool))
        {
        }

        public override ExpressionSyntax GetCreation(object obj)
            => SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
    }
}