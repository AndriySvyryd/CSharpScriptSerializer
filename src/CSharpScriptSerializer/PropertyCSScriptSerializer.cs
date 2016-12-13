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
            : this((Func<T, object>[]) null)
        {
        }

        public PropertyCSScriptSerializer(IReadOnlyCollection<Func<T, object>> constructorParameterGetters)
            : this(propertyConditions: null, constructorParameterGetters: constructorParameterGetters)
        {
        }

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
            : base(constructorParameterGetters)
        {
            propertyConditions = propertyConditions ?? new Dictionary<string, Func<T, object, bool>>();
            propertyValueGetters = propertyValueGetters ?? new Dictionary<string, Func<T, object>>();
            var referencedPropertyNames = propertyConditions.Keys.Concat(propertyValueGetters.Keys).Distinct();
            var allProperties = typeof(T).GetTypeInfo()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(p => p.Name);

            _propertyData = referencedPropertyNames.Select(n => allProperties[n])
                .Concat(allProperties.Values.Where(IsCandidateProperty)).Distinct()
                .Select(
                    p => new PropertyData(
                        p.Name,
                        p.PropertyType,
                        propertyValueGetters.GetValueOrDefault(p.Name, CreatePropertyInitializer(p)),
                        propertyConditions.GetValueOrDefault(p.Name,
                            (o, v) => !Equals(v, GetDefault(p.PropertyType)))))
                .ToArray();
        }

        protected override bool GenerateEmptyArgumentList => false;

        public override ExpressionSyntax GetCreation(object obj)
            => GetObjectCreationExpression((T) obj);

        protected override ObjectCreationExpressionSyntax GetObjectCreationExpression(T obj)
            => base.GetObjectCreationExpression(obj)
                .WithInitializer(AddNewLine(
                    SyntaxFactory.InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            ToCommaSeparatedList(_propertyData
                                .Where(p => p.PropertyCondition(obj, p.PropertyValueGetter(obj)))
                                .Select(p => SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName(p.PropertyName),
                                    GetCreationExpression(p.PropertyValueGetter(obj)))))))));

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
            public PropertyData(
                string propertyName,
                Type propertyType,
                Func<T, object> propertyValueGetter,
                Func<T, object, bool> propertyCondition)
            {
                PropertyName = propertyName;
                PropertyType = propertyType;
                PropertyValueGetter = propertyValueGetter;
                PropertyCondition = propertyCondition;
            }

            public string PropertyName { get; }
            public Type PropertyType { get; }
            public Func<T, object> PropertyValueGetter { get; }
            public Func<T, object, bool> PropertyCondition { get; }
        }
    }
}