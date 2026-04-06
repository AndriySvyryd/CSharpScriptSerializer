using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Defines the contract for converting a .NET object into a Roslyn C# expression syntax node
    ///     that, when evaluated as a script, recreates that object.
    /// </summary>
    public interface ICSScriptSerializer
    {
        /// <summary>
        ///     Returns the Roslyn expression syntax node that represents the creation of <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>An expression that, when evaluated, recreates <paramref name="obj"/>.</returns>
        ExpressionSyntax GetCreation(object obj);
    }
}