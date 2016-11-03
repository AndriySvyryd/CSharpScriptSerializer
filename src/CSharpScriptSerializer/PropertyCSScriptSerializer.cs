using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpScriptSerialization
{
    public class PropertyCSScriptSerializer<T> : ConstructorCSScriptSerializer<T>
    {
        private readonly IReadOnlyCollection<PropertyData> _propertyData;

        public PropertyCSScriptSerializer()
            : this((Func<T, object>[])null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyCollection<Func<T, object>> argumentGetters)
            : this(typeof(T).GetRuntimeProperties().Where(IsCandidateProperty), argumentGetters)
        {
        }

        public PropertyCSScriptSerializer(IEnumerable<PropertyInfo> properties)
            : this(properties, parameterGetters: null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object>> propertyValueGetters)
            : this(propertyValueGetters, parameterGetters: null)
        {
        }

        public PropertyCSScriptSerializer(IEnumerable<PropertyInfo> properties,
            IReadOnlyCollection<Func<T, object>> parameterGetters)
            : this(
                properties.Select(p => new PropertyData(p.Name, p.PropertyType, CreatePropertyInitializer(p))).ToArray(),
                parameterGetters)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyDictionary<string, Func<T, object>> propertyValueGetters,
            IReadOnlyCollection<Func<T, object>> parameterGetters)
            : base(parameterGetters)
        {
            var properties = typeof(T).GetTypeInfo()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .ToDictionary(p => p.Name);
            _propertyData = propertyValueGetters.Select(p => new PropertyData(p.Key, properties[p.Key].PropertyType, p.Value)).ToArray();
        }

        protected PropertyCSScriptSerializer(IReadOnlyCollection<PropertyData> propertyData,
            IReadOnlyCollection<Func<T, object>> parameterGetters)
            : base(parameterGetters)
        {
            _propertyData = propertyData;
        }

        protected override bool GenerateEmptyArgumentList => false;

        public override ExpressionSyntax GetCreation(object obj)
            => GetObjectCreationExpression((T)obj);

        // TODO: custom defaults
        protected override ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj)
            => base.GetObjectCreationExpression(obj)
                .WithInitializer(AddNewLine(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            ToCommaSeparatedList(_propertyData
                                .Select(p => new {p.PropertyName, p.PropertyType, PropertyValue = p.PropertyInitializer(obj)})
                                .Where(p => !IsDefault(p.PropertyValue, p.PropertyType))
                                .Select(p =>
                                    SyntaxFactory.AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        SyntaxFactory.IdentifierName(p.PropertyName),
                                        GetCreationExpression(p.PropertyValue))))))));

        protected static Func<T, object> CreatePropertyInitializer(PropertyInfo property)
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
            public PropertyData(string propertyName, Type propertyType, Func<T, object> propertyInitializer)
            {
                PropertyName = propertyName;
                PropertyType = propertyType;
                PropertyInitializer = propertyInitializer;
            }

            public string PropertyName { get; }
            public Type PropertyType { get; }
            public Func<T, object> PropertyInitializer { get; }
        }
    }
}