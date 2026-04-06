using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Serializes objects of type <typeparamref name="T"/> as a C# object initializer expression
    ///     using the type's public writable properties.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    public class PropertyCSScriptSerializer<T> : ConstructorCSScriptSerializer<T>
    {
        private readonly IReadOnlyCollection<PropertyData> _propertyData;
        private readonly IReadOnlyCollection<PropertyData> _hiddenPropertyData;

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> using default serialization
        ///     conditions for all public writable properties.
        /// </summary>
        public PropertyCSScriptSerializer()
            : this((Func<T, object>[])null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> with constructor parameters
        ///     and default serialization conditions for all public writable properties.
        /// </summary>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(propertyConditions: null, constructorParameterGetters: constructorParameterGetters)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> that excludes the specified
        ///     properties from serialization.
        /// </summary>
        /// <param name="ignoredProperties">
        ///     The names of the public writable properties to exclude from serialization.
        ///     Throws <see cref="InvalidOperationException"/> if any name does not correspond
        ///     to a public writable property on <typeparamref name="T"/>.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyCollection<string> ignoredProperties)
            : this(
                ignoredProperties?.ToDictionary<string, string, Func<T, object, bool>>(
                    p => p, p => (o, v) => false),
                constructorParameterGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> with per-property serialization conditions.
        /// </summary>
        /// <param name="propertyConditions">
        ///     A dictionary mapping property names to functions that determine whether each property should be serialized,
        ///     given the object and the current property value. Properties not present in this dictionary use a default
        ///     condition that serializes the property only when its value differs from the type default.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions)
            : this(propertyConditions, constructorParameterGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> with per-property serialization
        ///     conditions and constructor parameters.
        /// </summary>
        /// <param name="propertyConditions">
        ///     A dictionary mapping property names to functions that determine whether each property should be serialized.
        ///     Properties not present in this dictionary use a default condition.
        /// </param>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(propertyConditions, constructorParameterGetters, propertyValueGetters: null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> with per-property serialization
        ///     conditions, constructor parameters, and custom property value getters.
        /// </summary>
        /// <param name="propertyConditions">
        ///     A dictionary mapping property names to functions that determine whether each property should be serialized.
        ///     Properties not present in this dictionary use a default condition.
        /// </param>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        /// <param name="propertyValueGetters">
        ///     A dictionary mapping property names to functions that return the value to serialize for each property,
        ///     overriding the default property getter.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters,
            IReadOnlyDictionary<string, Func<T, object>> propertyValueGetters)
            : this(propertyConditions, constructorParameterGetters, propertyValueGetters, null, null)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer{T}"/> with full control over property
        ///     serialization for both visible and hidden (shadowed) properties.
        /// </summary>
        /// <param name="propertyConditions">
        ///     A dictionary mapping property names to functions that determine whether each property should be serialized.
        ///     Properties not present in this dictionary use a default condition.
        /// </param>
        /// <param name="constructorParameterGetters">
        ///     Getters for values passed as positional constructor arguments when creating <typeparamref name="T"/>.
        /// </param>
        /// <param name="propertyValueGetters">
        ///     A dictionary mapping property names to functions that return the value to serialize for each property.
        /// </param>
        /// <param name="hiddenPropertyConditions">
        ///     A dictionary mapping <c>DeclaringTypeName.PropertyName</c> keys to functions that determine whether
        ///     each hidden (shadowed) base-class property should be serialized.
        /// </param>
        /// <param name="hiddenPropertyValueGetters">
        ///     A dictionary mapping <c>DeclaringTypeName.PropertyName</c> keys to functions that return the value
        ///     to serialize for each hidden (shadowed) base-class property.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters,
            IReadOnlyDictionary<string, Func<T, object>> propertyValueGetters,
            IReadOnlyDictionary<string, Func<T, object, bool>> hiddenPropertyConditions,
            IReadOnlyDictionary<string, Func<T, object>> hiddenPropertyValueGetters)
            : base(constructorParameterGetters)
        {
            var typeInfo = typeof(T).GetTypeInfo();
            var allUsableProperties = new Dictionary<string, PropertyInfo>();
            var allHiddenProperties = new Dictionary<string, PropertyInfo>();
            while (!typeInfo.Equals(typeof(object)))
            {
                foreach (var property in typeInfo
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (!IsUsableProperty(property)
                        || !Equals(property.SetMethod.GetBaseDefinition(), property.SetMethod))
                    {
                        continue;
                    }

                    if (!allUsableProperties.ContainsKey(property.Name))
                    {
                        allUsableProperties[property.Name] = property;
                    }
                    else
                    {
                        allHiddenProperties[property.DeclaringType.Name + "." + property.Name] = property;
                    }
                }

                typeInfo = typeInfo.BaseType.GetTypeInfo();
            }

            propertyConditions ??= new Dictionary<string, Func<T, object, bool>>();
            propertyValueGetters ??= new Dictionary<string, Func<T, object>>();

            _propertyData =
                GetProperties(propertyConditions.Keys.Concat(propertyValueGetters.Keys).Distinct(),
                    allUsableProperties, hidden: false)
                    .Concat(allUsableProperties.Values.Where(IsCandidateProperty)).Distinct()
                    .Select(p =>
                    {
                        var propertyType = p.PropertyType;
                        return new PropertyData(
                            p.Name,
                            propertyType,
                            p.DeclaringType,
                            propertyValueGetters.GetValueOrDefault(p.Name,
                                CreatePropertyValueGetter(p)),
                            propertyConditions.GetValueOrDefault(p.Name,
                                (o, v) => !Equals(v, GetDefault(propertyType))));
                    })
                    .ToArray();

            hiddenPropertyConditions ??= new Dictionary<string, Func<T, object, bool>>();
            hiddenPropertyValueGetters ??= new Dictionary<string, Func<T, object>>();

            _hiddenPropertyData =
                GetProperties(hiddenPropertyConditions.Keys.Concat(hiddenPropertyValueGetters.Keys).Distinct(),
                    allHiddenProperties, hidden: true)
                    .Concat(allHiddenProperties.Values.Where(IsCandidateProperty)).Distinct()
                    .Select(p =>
                    {
                        var propertyType = p.PropertyType;
                        return new PropertyData(
                            p.Name,
                            propertyType,
                            p.DeclaringType,
                            hiddenPropertyValueGetters.GetValueOrDefault(p.DeclaringType.Name + "." + p.Name,
                                CreatePropertyValueGetter(p)),
                            hiddenPropertyConditions.GetValueOrDefault(p.DeclaringType.Name + "." + p.Name,
                                (o, v) => !Equals(v, GetDefault(propertyType))));
                    })
                    .ToArray();
        }

        protected override bool GenerateEmptyArgumentList => false;

        private IEnumerable<PropertyInfo> GetProperties(
            IEnumerable<string> propertyNames,
            Dictionary<string, PropertyInfo> allProperties,
            bool hidden)
        {
            foreach (var propertyName in propertyNames)
            {
                if (!allProperties.TryGetValue(propertyName, out var property))
                {
                    if (hidden)
                    {
                        throw new InvalidOperationException(
                            $"The type {typeof(T)} does not have a hidden public nonstatic writable property {propertyName}");
                    }

                    throw new InvalidOperationException(
                        $"The type {typeof(T)} does not have a public nonstatic writable property {propertyName}");
                }
                yield return property;
            }
        }

        public override ExpressionSyntax GetCreation(object obj)
        {
            var typedObject = (T)obj;
            var objectParameter = IdentifierName("o");
            var hiddenPropertyInitializers = _hiddenPropertyData
                .Where(p => p.PropertyCondition(typedObject, p.PropertyValueGetter(typedObject)))
                .Select(p => (StatementSyntax)ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ParenthesizedExpression(CastExpression(GetTypeSyntax(p.DeclaringType), objectParameter)),
                        IdentifierName(p.PropertyName)),
                    GetCreationExpression(p.PropertyValueGetter(typedObject)))))
                .ToList();

            var objectCreationExpression = GetObjectCreationExpression(typedObject);
            if (hiddenPropertyInitializers.Count == 0)
            {
                return objectCreationExpression;
            }

            hiddenPropertyInitializers.Add(ReturnStatement(objectParameter));
            return InvocationExpression(
                ParenthesizedExpression(
                    CastExpression(
                        GenericName(Identifier("Func")).WithTypeArgumentList(
                            TypeArgumentList(
                                SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[]
                                {
                                    GetTypeSyntax(Type), Token(SyntaxKind.CommaToken),
                                    GetTypeSyntax(Type)
                                }))),
                        ParenthesizedExpression(SimpleLambdaExpression(Parameter(Identifier("o")),
                            Block(hiddenPropertyInitializers))))))
                .WithArgumentList(ArgumentList(SingletonSeparatedList(Argument(objectCreationExpression))));
        }

        protected override ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj)
        {
            var properties = _propertyData
                .Where(p => p.PropertyCondition(obj, p.PropertyValueGetter(obj)))
                .Select(p => AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(p.PropertyName),
                    GetCreationExpression(p.PropertyValueGetter(obj)))).ToList();

            var expression = properties.Count == 0
                ? base.GetObjectCreationExpression(obj, generateEmptyArgumentList: true)
                : base.GetObjectCreationExpression(obj)
                    .WithInitializer(AddNewLine(
                        InitializerExpression(
                            SyntaxKind.ObjectInitializerExpression,
                            SeparatedList<ExpressionSyntax>(
                                ToCommaSeparatedList(properties)))));
            
            return expression;
        }

        protected static Func<T, object> CreatePropertyValueGetter(PropertyInfo property)
        {
            var objectParameter = Expression.Parameter(typeof(T), name: "o");
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(
                    Expression.MakeMemberAccess(objectParameter, property),
                    typeof(object)),
                objectParameter)
                .Compile();
        }

        protected class PropertyData
        {
            public PropertyData(
                string propertyName,
                Type propertyType,
                Type declaringType,
                Func<T, object> propertyValueGetter,
                Func<T, object, bool> propertyCondition)
            {
                PropertyName = propertyName;
                PropertyType = propertyType;
                DeclaringType = declaringType;
                PropertyValueGetter = propertyValueGetter;
                PropertyCondition = propertyCondition;
            }

            public string PropertyName { get; }
            public Type PropertyType { get; }
            public Type DeclaringType { get; }
            public Func<T, object> PropertyValueGetter { get; }
            public Func<T, object, bool> PropertyCondition { get; }
        }
    }
}