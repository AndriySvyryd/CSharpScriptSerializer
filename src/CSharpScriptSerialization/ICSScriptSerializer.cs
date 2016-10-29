using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    public interface ICSScriptSerializer
    {
        ExpressionSyntax GetCreation(object obj);
    }
}