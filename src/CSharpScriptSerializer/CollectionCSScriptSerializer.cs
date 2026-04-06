using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes a collection of type <typeparamref name="T"/> as a constructor invocation with
    ///     a collection initializer expression.
    /// </summary>
    /// <typeparam name="T">The collection type to serialize.</typeparam>
    public class CollectionCSScriptSerializer<T> : ConstructorCSScriptSerializer<T>
    {
        private readonly IReadOnlyCollection<Func<object, object>> _elementDecomposers;
        private readonly Func<T, IEnumerable<object>> _getEnumerable;

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> that serializes each element
        ///     directly, with no constructor arguments and using <see cref="System.Collections.IEnumerable"/> to enumerate.
        /// </summary>
        public CollectionCSScriptSerializer()
            : this(elementDecomposers: null, constructorParameterGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with custom element decomposers.
        /// </summary>
        /// <param name="elementDecomposers">
        ///     Functions that decompose each element into sub-values for a complex collection initializer entry
        ///     (e.g., key and value for a dictionary). Pass <see langword="null"/> or empty to serialize each element directly.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers)
            : this(elementDecomposers, constructorParameterGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with strongly-typed constructor
        ///     parameter getters.
        /// </summary>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(elementDecomposers: null, constructorParameterGetters: constructorParameterGetters)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with element decomposers
        ///     and a non-generic enumerable getter.
        /// </summary>
        /// <param name="elementDecomposers">
        ///     Functions that decompose each element into sub-values for a complex collection initializer entry.
        /// </param>
        /// <param name="getEnumerable">
        ///     A function that extracts the sequence of elements from an instance of <typeparamref name="T"/>.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            Func<object, IEnumerable<object>> getEnumerable)
            : this(elementDecomposers, nonGenericParameterGetters: null, nonGenericGetEnumerable: getEnumerable)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with strongly-typed constructor
        ///     parameter getters and an enumerable getter.
        /// </summary>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        /// <param name="getEnumerable">
        ///     A function that extracts the sequence of elements from an instance of <typeparamref name="T"/>.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters,
            Func<T, IEnumerable<object>> getEnumerable)
            : this(
                elementDecomposers: null,
                constructorParameterGetters: constructorParameterGetters,
                getEnumerable: getEnumerable)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with element decomposers
        ///     and non-generic constructor parameter getters.
        /// </summary>
        /// <param name="elementDecomposers">
        ///     Functions that decompose each element into sub-values for a complex collection initializer entry.
        /// </param>
        /// <param name="nonGenericParameterGetters">
        ///     Non-generic getters for values passed as positional constructor arguments.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            IReadOnlyCollection<Func<object, object>> nonGenericParameterGetters)
            : this(elementDecomposers, nonGenericParameterGetters, nonGenericGetEnumerable: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="CollectionCSScriptSerializer{T}"/> with element decomposers
        ///     and strongly-typed constructor parameter getters.
        /// </summary>
        /// <param name="elementDecomposers">
        ///     Functions that decompose each element into sub-values for a complex collection initializer entry.
        /// </param>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(elementDecomposers, constructorParameterGetters, getEnumerable: null)
        {
        }

        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            IReadOnlyCollection<Func<object, object>> nonGenericParameterGetters,
            Func<object, IEnumerable<object>> nonGenericGetEnumerable)
            : this(nonGenericParameterGetters)
        {
            _elementDecomposers = elementDecomposers;
            _getEnumerable = nonGenericGetEnumerable != null
                ? (Func<T, IEnumerable<object>>)(o => nonGenericGetEnumerable(o))
                : (o => ((IEnumerable)o).Cast<object>());
        }

        public CollectionCSScriptSerializer(
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters,
            Func<T, IEnumerable<object>> getEnumerable)
            : base(constructorParameterGetters)
        {
            _elementDecomposers = elementDecomposers;
            _getEnumerable = getEnumerable ?? (o => ((IEnumerable)o).Cast<object>());
        }

        protected override bool GenerateEmptyArgumentList => false;

        public override ExpressionSyntax GetCreation(object obj) => GetObjectCreationExpression((T)obj);

        protected override ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj)
        {
            var list = _getEnumerable(obj).ToList();
            return list.Count == 0
                ? base.GetObjectCreationExpression(obj)
                    .WithArgumentList(SyntaxFactory.ArgumentList())
                : base.GetObjectCreationExpression(obj)
                    .WithInitializer(AddNewLine(
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.CollectionInitializerExpression,
                            SyntaxFactory.SeparatedList<ExpressionSyntax>(
                                ToCommaSeparatedList(list
                                    .Select(GetElementExpression))))));
        }

        protected virtual ExpressionSyntax GetElementExpression(object element)
            => _elementDecomposers == null
               || _elementDecomposers.Count == 0
                ? GetCreationExpression(element)
                : _elementDecomposers.Count == 1
                    ? GetCreationExpression(_elementDecomposers.First()(element))
                    : SyntaxFactory.InitializerExpression(
                        SyntaxKind.ComplexElementInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            ToCommaSeparatedList(_elementDecomposers
                                .Select(getSubelement => GetCreationExpression(getSubelement(element))))));
    }
}