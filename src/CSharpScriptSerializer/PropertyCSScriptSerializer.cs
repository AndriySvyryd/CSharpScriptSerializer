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
    public class PropertyCSScriptSerializer<T> : ConstructorCSScriptSerializer<T>
    {
        private readonly IReadOnlyCollection<PropertyData> _propertyData;
        private readonly IReadOnlyCollection<PropertyData> _hiddenPropertyData;

        public PropertyCSScriptSerializer()
            : this((Func<T, object>[])null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(propertyConditions: null, constructorParameterGetters: constructorParameterGetters)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="PropertyCSScriptSerializer"/>.
        /// </summary>
        /// <param name="propertyConditions">
        ///     A collection of functions that determine whether the corresponding property should be serialized.
        ///     If an entry is missing for any public property a default one will be created.
        /// </param>
        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions)
            : this(propertyConditions, constructorParameterGetters: null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(propertyConditions, constructorParameterGetters, propertyValueGetters: null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object, bool>> propertyConditions,
            IReadOnlyCollection<Func<T, object>> constructorParameterGetters,
            IReadOnlyDictionary<string, Func<T, object>> propertyValueGetters)
            : this(propertyConditions, constructorParameterGetters, propertyValueGetters, null, null)
        {
        }

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
                    .Select(p => new PropertyData(
                        p.Name,
                        p.PropertyType,
                        p.DeclaringType,
                        propertyValueGetters.GetValueOrDefault(p.Name,
                            CreatePropertyValueGetter(p)),
                        propertyConditions.GetValueOrDefault(p.Name,
                            (o, v) => !Equals(v, GetDefault(p.PropertyType)))))
                    .ToArray();

            hiddenPropertyConditions ??= new Dictionary<string, Func<T, object, bool>>();
            hiddenPropertyValueGetters ??= new Dictionary<string, Func<T, object>>();

            _hiddenPropertyData =
                GetProperties(hiddenPropertyConditions.Keys.Concat(hiddenPropertyValueGetters.Keys).Distinct(),
                    allHiddenProperties, hidden: true)
                    .Concat(allHiddenProperties.Values.Where(IsCandidateProperty)).Distinct()
                    .Select(p => new PropertyData(
                        p.Name,
                        p.PropertyType,
                        p.DeclaringType,
                        hiddenPropertyValueGetters.GetValueOrDefault(p.DeclaringType.Name + "." + p.Name,
                            CreatePropertyValueGetter(p)),
                        hiddenPropertyConditions.GetValueOrDefault(p.DeclaringType.Name + "." + p.Name,
                            (o, v) => !Equals(v, GetDefault(p.PropertyType)))))
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

            var expression = base.GetObjectCreationExpression(obj, generateEmptyArgumentList: properties.Count == 0)
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