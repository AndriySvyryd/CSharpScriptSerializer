using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    public interface ICSScriptSerializable
    {
        ExpressionSyntax GetCreation();
    }
}