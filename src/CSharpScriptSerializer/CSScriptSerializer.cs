using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Scripting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpScriptSerialization
{
    public abstract class CSScriptSerializer : ICSScriptSerializer
    {
        protected CSScriptSerializer(Type type)
        {
            Type = type;
        }

        public Type Type { get; }

        public abstract ExpressionSyntax GetCreation(object obj);

        public static readonly List<ICSScriptSerializerFactory> SerializerFactories =
            new List<ICSScriptSerializerFactory>();

        public static readonly ConcurrentDictionary<Type, ICSScriptSerializer> Serializers =
            new ConcurrentDictionary<Type, ICSScriptSerializer>();

        public static T Deserialize<T>(string script)
            => DeserializeAsync<T>(script).GetAwaiter().GetResult();

        public static Task<T> DeserializeAsync<T>(string script)
            => DeserializeAsync<T>(script, Enumerable.Empty<Assembly>(), Enumerable.Empty<string>());

        public static T Deserialize<T>(string script, IEnumerable<Assembly> referencedAssemblies, IEnumerable<string> imports)
            => DeserializeAsync<T>(script, referencedAssemblies, imports).GetAwaiter().GetResult();

        public static Task<T> DeserializeAsync<T>(string script, IEnumerable<Assembly> referencedAssemblies, IEnumerable<string> imports)
            => CSharpScript.EvaluateAsync<T>(script,
                ScriptOptions.Default.WithReferences(
                    typeof(T).GetTypeInfo().Assembly,
                    typeof(List<>).GetTypeInfo().Assembly
#if !NET46
                    , Assembly.Load(new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"))
#endif
                    )
                    .AddReferences(typeof(CSScriptSerializer).GetTypeInfo()
                        .Assembly.GetReferencedAssemblies()
                        .Select(Assembly.Load))
                    .AddImports(
                        typeof(T).GetTypeInfo().Namespace,
                        typeof(DateTime).GetTypeInfo().Namespace,
                        typeof(List<>).GetTypeInfo().Namespace));

        public static string Serialize(object obj)
        {
            using (var workspace = new AdhocWorkspace())
            {
                return Formatter.Format(
                    GetCompilationUnitExpression(obj),
                    workspace,
                    workspace.Options)
                    .ToFullString();
            }
        }

        public static CompilationUnitSyntax GetCompilationUnitExpression(object obj)
            => CompilationUnit()
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        GlobalStatement(
                            ExpressionStatement(GetCreationExpression(obj))
                                .WithSemicolonToken(MissingToken(SyntaxKind.SemicolonToken)))));

        public static ExpressionSyntax GetCreationExpression(object obj)
        {
            var serializable = obj as ICSScriptSerializable;
            return serializable != null
                ? serializable.GetCreation()
                : GetSerializer(obj).GetCreation(obj);
        }

        private static ICSScriptSerializer GetSerializer(object obj)
        {
            if (obj == null)
            {
                return NullCSScriptSerializer.Instance;
            }

            var type = UnwrapNullableType(obj.GetType());
            if (type == typeof(bool))
            {
                return (bool)obj
                    ? TrueCSScriptSerializer.Instance
                    : (CSScriptSerializer)FalseCSScriptSerializer.Instance;
            }

            return Serializers.GetOrAdd(type, CreateSerializer);
        }

        private static ICSScriptSerializer CreateSerializer(Type type)
        {
            if (type == typeof(string))
            {
                return new LiteralCSScriptSerializer(type, SyntaxKind.StringLiteralExpression);
            }

            if (type == typeof(char))
            {
                return new LiteralCSScriptSerializer(type, SyntaxKind.CharacterLiteralExpression);
            }

            if (type == typeof(decimal)
                || type == typeof(double)
                || type == typeof(float)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(short)
                || type == typeof(byte)
                || type == typeof(ushort)
                || type == typeof(sbyte))
            {
                return new LiteralCSScriptSerializer(type, SyntaxKind.NumericLiteralExpression);
            }

            if (type.GetTypeInfo().IsEnum)
            {
                return new EnumCSScriptSerializer(type);
            }

            if (type == typeof(Guid))
            {
                return new ConstructorCSScriptSerializer<Guid>(
                    new Func<Guid, object>[] {g => g.ToString()});
            }

            if (type == typeof(DateTime))
            {
                return new ConstructorCSScriptSerializer<DateTime>(
                    new Func<DateTime, object>[] {d => d.Ticks, d => d.Kind});
            }

            if (type == typeof(DateTimeOffset))
            {
                return new ConstructorCSScriptSerializer<DateTimeOffset>(
                    new Func<DateTimeOffset, object>[] {d => d.DateTime, d => d.Offset});
            }

            if (type == typeof(TimeSpan))
            {
                return new ConstructorCSScriptSerializer<TimeSpan>(
                    new Func<TimeSpan, object>[] {t => t.Ticks});
            }

            if (type.IsArray)
            {
                return new ArrayCSScriptSerializer(type);
            }

            if (type.IsConstructedGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                if (genericDefinition == typeof(Tuple<>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 1));
                }
                if (genericDefinition == typeof(Tuple<,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 2));
                }
                if (genericDefinition == typeof(Tuple<,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 3));
                }
                if (genericDefinition == typeof(Tuple<,,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 4));
                }
                if (genericDefinition == typeof(Tuple<,,,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 5));
                }
                if (genericDefinition == typeof(Tuple<,,,,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 6));
                }
                if (genericDefinition == typeof(Tuple<,,,,,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 7));
                }
                if (genericDefinition == typeof(Tuple<,,,,,,,>))
                {
                    return CreateConstructorCSScriptSerializer(type, CreateTupleGetters(type, arity: 8));
                }
            }

            foreach (var additionalSerializerFactory in SerializerFactories)
            {
                var serializer = additionalSerializerFactory.TryCreate(type);
                if (serializer != null)
                {
                    return serializer;
                }
            }

            if (!IsConstructable(type))
            {
                throw new InvalidOperationException($"The type {type} does not have a public parameterless constructor");
            }

            var typeInfo = type.GetTypeInfo();
            if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(typeInfo))
            {
                return CreateCollectionCSScriptSerializer(
                    type,
                    new Func<object, object>[] {p => ((DictionaryEntry)p).Key, p => ((DictionaryEntry)p).Value},
                    o => ToEnumerable(((IDictionary)o).GetEnumerator()));
            }

            if (typeof(ICollection).GetTypeInfo().IsAssignableFrom(typeInfo)
                || TryGetElementType(type, typeof(ICollection<>)) != null)
            {
                return (CSScriptSerializer)GetDeclaredConstructor(
                    typeof(CollectionCSScriptSerializer<>).MakeGenericType(type), types: null)
                    .Invoke(parameters: null);
            }

            if (!IsInitializable(type))
            {
                throw new InvalidOperationException($"The type {type} does not have public writable properties");
            }

            // TODO: record types being constructed to avoid recursion
            return (CSScriptSerializer)GetDeclaredConstructor(
                typeof(PropertyCSScriptSerializer<>).MakeGenericType(type), types: null)
                .Invoke(parameters: null);
        }

        private static IEnumerable<object> ToEnumerable(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private static CSScriptSerializer CreateConstructorCSScriptSerializer(
            Type type,
            IReadOnlyCollection<Func<object, object>> parameterGetters)
            => (CSScriptSerializer)GetDeclaredConstructor(
                typeof(ConstructorCSScriptSerializer<>).MakeGenericType(type),
                new[] {typeof(IReadOnlyCollection<Func<object, object>>)})
                .Invoke(new object[] {parameterGetters});

        private static CSScriptSerializer CreateCollectionCSScriptSerializer(
            Type type,
            IReadOnlyCollection<Func<object, object>> elementDecomposers,
            Func<object, IEnumerable<object>> getEnumerable)
            => (CSScriptSerializer)GetDeclaredConstructor(
                typeof(CollectionCSScriptSerializer<>).MakeGenericType(type),
                new[] {typeof(IReadOnlyCollection<Func<object, object>>), typeof(Func<object, IEnumerable<object>>)})
                .Invoke(new object[] {elementDecomposers, getEnumerable});

        private static Func<object, object>[] CreateTupleGetters(Type type, int arity)
        {
            var getters = new List<Func<object, object>>();
            for (var i = 1; i <= arity; i++)
            {
                var itemProperty = type.GetTypeInfo().GetProperty("Item" + i);
                getters.Add(o => itemProperty.GetValue(o));
            }
            return getters.ToArray();
        }

        protected static bool IsConstructable(Type type)
            => !type.GetTypeInfo().IsInterface
               && !type.GetTypeInfo().IsAbstract
               && !type.GetTypeInfo().IsGenericTypeDefinition
               && (type.GetTypeInfo().IsValueType
                   || GetDeclaredConstructor(type, types: null) != null);

        protected static bool IsInitializable(Type type) => type.GetRuntimeProperties().Where(IsCandidateProperty).Any();

        protected static bool IsCandidateProperty(PropertyInfo property)
            => !(property.GetMethod ?? property.SetMethod).IsStatic
               && property.GetIndexParameters().Length == 0
               && property.CanRead
               && property.CanWrite
               && property.GetMethod != null
               && property.GetMethod.IsPublic
               && property.SetMethod != null
               && property.SetMethod.IsPublic;

        protected static Type UnwrapNullableType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        protected static Type TryGetElementType(Type type, Type interfaceOrBaseType)
        {
            var types = GetGenericTypeImplementations(type, interfaceOrBaseType).ToList();
            return types.Count == 1 ? types[index: 0].GetTypeInfo().GenericTypeArguments.FirstOrDefault() : null;
        }

        private static readonly ConcurrentDictionary<Type, object> TypeDefaults =
            new ConcurrentDictionary<Type, object>();

        protected static object GetDefault(Type type)
            => type.GetTypeInfo().IsValueType ? TypeDefaults.GetOrAdd(type, Activator.CreateInstance) : null;

        protected static bool IsDefault(object obj)
            => IsDefault(obj, obj.GetType());

        protected static bool IsDefault(object obj, Type type)
            => obj == null || obj.Equals(GetDefault(type));

        protected static IEnumerable<Type> GetGenericTypeImplementations(Type type, Type interfaceOrBaseType)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericTypeDefinition)
            {
                return (interfaceOrBaseType.GetTypeInfo().IsInterface
                    ? typeInfo.ImplementedInterfaces
                    : GetBaseTypes(type))
                    .Union(new[] {type})
                    .Where(t => t.GetTypeInfo().IsGenericType
                                && (t.GetGenericTypeDefinition() == interfaceOrBaseType));
            }

            return Enumerable.Empty<Type>();
        }

        protected static IEnumerable<Type> GetBaseTypes(Type type)
        {
            type = type.GetTypeInfo().BaseType;
            while (type != null)
            {
                yield return type;

                type = type.GetTypeInfo().BaseType;
            }
        }

        protected static ConstructorInfo GetDeclaredConstructor(Type type, Type[] types)
            => type.GetTypeInfo().DeclaredConstructors
                .SingleOrDefault(
                    c => !c.IsStatic
                         && c.GetParameters().Where(p => !p.ParameterType.IsGenericParameter)
                             .Select(p => p.ParameterType).SequenceEqual(types ?? new Type[0]));

        protected static IEnumerable<SyntaxNodeOrToken> ToCommaSeparatedList(IEnumerable<CSharpSyntaxNode> tokens)
            => tokens.Aggregate(new List<SyntaxNodeOrToken>(),
                (list, current) =>
                {
                    if (list.Count > 0)
                    {
                        list.Add(Token(SyntaxKind.CommaToken));
                    }
                    list.Add(current);
                    return list;
                });

        protected static TypeSyntax GetTypeSyntax(Type type)
        {
            if (type == typeof(bool))
            {
                return PredefinedType(Token(SyntaxKind.BoolKeyword));
            }
            if (type == typeof(byte))
            {
                return PredefinedType(Token(SyntaxKind.ByteKeyword));
            }
            if (type == typeof(sbyte))
            {
                return PredefinedType(Token(SyntaxKind.SByteKeyword));
            }
            if (type == typeof(char))
            {
                return PredefinedType(Token(SyntaxKind.CharKeyword));
            }
            if (type == typeof(short))
            {
                return PredefinedType(Token(SyntaxKind.ShortKeyword));
            }
            if (type == typeof(ushort))
            {
                return PredefinedType(Token(SyntaxKind.UShortKeyword));
            }
            if (type == typeof(int))
            {
                return PredefinedType(Token(SyntaxKind.IntKeyword));
            }
            if (type == typeof(uint))
            {
                return PredefinedType(Token(SyntaxKind.UIntKeyword));
            }
            if (type == typeof(long))
            {
                return PredefinedType(Token(SyntaxKind.LongKeyword));
            }
            if (type == typeof(ulong))
            {
                return PredefinedType(Token(SyntaxKind.ULongKeyword));
            }
            if (type == typeof(float))
            {
                return PredefinedType(Token(SyntaxKind.FloatKeyword));
            }
            if (type == typeof(double))
            {
                return PredefinedType(Token(SyntaxKind.DoubleKeyword));
            }
            if (type == typeof(decimal))
            {
                return PredefinedType(Token(SyntaxKind.DecimalKeyword));
            }
            if (type == typeof(string))
            {
                return PredefinedType(Token(SyntaxKind.StringKeyword));
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                return NullableType(GetTypeSyntax(underlyingType));
            }
            if (type.IsArray)
            {
                return ArrayType(GetTypeSyntax(GetArrayElementType(type)))
                    .WithRankSpecifiers(List(
                        GetArrayRanks(type)));
            }

            return GetNameSyntax(type, null);
        }

        private static NameSyntax GetNameSyntax(Type type, List<Type> genericArguments)
        {
            var typeInfo = type.GetTypeInfo();
            genericArguments = genericArguments ?? typeInfo.GenericTypeArguments.ToList();

            var declaringTypeGenericArguments = new List<Type>();
            var declaringType = type.DeclaringType;
            if (declaringType != null)
            {
                var genericParameters = declaringType.GetTypeInfo().GenericTypeParameters;
                declaringTypeGenericArguments = genericArguments.GetRange(0, genericParameters.Length);
                genericArguments.RemoveRange(0, genericParameters.Length);
            }

            var simpleName = genericArguments.Count > 0
                ? GenericName(
                    Identifier(type.Name.Substring(startIndex: 0,
                        length: type.Name.IndexOf(value: '`'))))
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(
                                ToCommaSeparatedList(genericArguments.Select(GetTypeSyntax)))))
                : (SimpleNameSyntax)IdentifierName(type.Name);

            if (declaringType == null
                || typeInfo.IsGenericParameter)
            {
                return simpleName;
            }

            return QualifiedName(GetNameSyntax(declaringType, declaringTypeGenericArguments), simpleName);
        }

        private static Type GetArrayElementType(Type type)
            => type.IsArray ? GetArrayElementType(type.GetElementType()) : type;

        private static IEnumerable<ArrayRankSpecifierSyntax> GetArrayRanks(Type type)
            => type == null || !type.IsArray
                ? Enumerable.Empty<ArrayRankSpecifierSyntax>()
                : Enumerable.Repeat(ArrayRankSpecifier(
                    SeparatedList<ExpressionSyntax>(
                        ToCommaSeparatedList(
                            Enumerable.Repeat(OmittedArraySizeExpression(), type.GetArrayRank())))),
                    count: 1)
                    .Concat(GetArrayRanks(type.GetElementType()));

        protected static TSyntax AddNewLine<TSyntax>(TSyntax expression)
            where TSyntax : SyntaxNode
            => expression.FullSpan.Length > 120
                ? expression.WithLeadingTrivia(CarriageReturnLineFeed)
                    .WithTrailingTrivia(CarriageReturnLineFeed)
                : expression;
    }
}