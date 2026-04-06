using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes objects of type <typeparamref name="T"/> as a constructor invocation expression,
    ///     passing values derived from the object as positional arguments.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    public class ConstructorCSScriptSerializer<T> : CSScriptSerializer
    {
        private readonly IReadOnlyCollection<Func<T, object>> _constructorParameterGetters;
        private ObjectCreationExpressionSyntax _objectCreationExpression;

        /// <summary>
        ///     Creates a new instance of <see cref="ConstructorCSScriptSerializer{T}"/> that emits a
        ///     no-argument constructor call.
        /// </summary>
        public ConstructorCSScriptSerializer()
            : this(constructorParameterGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="ConstructorCSScriptSerializer{T}"/> using non-generic
        ///     parameter getters.
        /// </summary>
        /// <param name="nonGenericParameterGetters">
        ///     Functions that accept the object as <see cref="object"/> and return the value for each
        ///     positional constructor argument.
        /// </param>
        public ConstructorCSScriptSerializer(IReadOnlyCollection<Func<object, object>> nonGenericParameterGetters)
            : this(nonGenericParameterGetters
                .Select<Func<object, object>, Func<T, object>>(g => o => g(o)).ToArray())
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="ConstructorCSScriptSerializer{T}"/> using strongly-typed
        ///     parameter getters.
        /// </summary>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        ///     Pass <see langword="null"/> or an empty collection to emit a no-argument constructor call.
        /// </param>
        public ConstructorCSScriptSerializer(IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : base(typeof(T)) => _constructorParameterGetters = constructorParameterGetters;

        /// <summary>
        ///     Gets a value indicating whether to emit an empty argument list <c>()</c> when no constructor
        ///     parameters are provided. Defaults to <see langword="true"/>.
        /// </summary>
        protected virtual bool GenerateEmptyArgumentList => true;

        /// <inheritdoc/>
        public override ExpressionSyntax GetCreation(object obj) => GetObjectCreationExpression((T)obj);

        protected virtual ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj)
            => GetObjectCreationExpression(obj, GenerateEmptyArgumentList);

        protected virtual ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj, bool generateEmptyArgumentList)
            => _constructorParameterGetters == null
               || _constructorParameterGetters.Count == 0
                ? generateEmptyArgumentList
                    ? GetObjectCreationExpression().WithArgumentList(SyntaxFactory.ArgumentList())
                    : GetObjectCreationExpression()
                : GetObjectCreationExpression()
                    .WithArgumentList(AddNewLine(SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList<ArgumentSyntax>(
                            ToCommaSeparatedList(
                                _constructorParameterGetters.Select(a =>
                                    SyntaxFactory.Argument(GetCreationExpression(a(obj)))))))));

        protected virtual ObjectCreationExpressionSyntax GetObjectCreationExpression()
            =>_objectCreationExpression ??= SyntaxFactory.ObjectCreationExpression(GetTypeSyntax(Type));
    }
}