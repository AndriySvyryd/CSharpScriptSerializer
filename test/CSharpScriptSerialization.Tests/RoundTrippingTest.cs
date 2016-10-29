using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CSharpScriptSerialization.Tests
{
    public class RoundtrippingTest
    {
        [Fact]
        public void SimpleStruct()
        {
            var input = new Point {X = 1, Y = 1};
            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<Point>(script);

            Assert.Equal(input.X, output.X);
            Assert.Equal(input.Y, output.Y);
        }

        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        [Fact]
        public void KnownTypes()
        {
            var dateTime = new DateTime(2000, 1, 1, 0, 0, 0);
            var dateTimeOffset = new DateTimeOffset(new DateTime(), TimeSpan.FromHours(-8.0));
            var timeSpan = new TimeSpan(0, 10, 9, 8, 7);
            var guid = Guid.NewGuid();
            var input = new AllSupportedTypes
            {
                Int16 = -1234,
                Int32 = -123456789,
                Int64 = -1234567890123456789L,
                Double = -1.23456789,
                Decimal = -1234567890.01M,
                DateTime = new DateTime(dateTime.Ticks, dateTime.Kind),
                DateTimeOffset = new DateTimeOffset(dateTimeOffset.DateTime, dateTimeOffset.Offset),
                TimeSpan = new TimeSpan(timeSpan.Ticks),
                String = "value",
                Single = -1.234F,
                Boolean = true,
                Byte = 255,
                UnsignedInt16 = 1234,
                UnsignedInt32 = 1234565789U,
                UnsignedInt64 = 1234567890123456789UL,
                Character = 'a',
                SignedByte = -128,
                Guid = new Guid(guid.ToString()),
                EnumInt32 = EnumInt32.SomeValue,
                FlagsEnum = FlagsEnum.FirstFlag | FlagsEnum.SecondFlag,
                NullableInt16 = -1234,
                NullableInt32 = null,
                NullableInt64 = -1234567890123456789L,
                NullableDouble = -1.23456789,
                NullableDecimal = -1234567890.01M,
                NullableDateTime = new DateTime(dateTime.Ticks, dateTime.Kind),
                NullableDateTimeOffset = new DateTimeOffset(dateTimeOffset.DateTime, dateTimeOffset.Offset),
                NullableTimeSpan = null,
                NullableSingle = -1.234F,
                NullableBoolean = null,
                NullableByte = 255,
                NullableUnsignedInt16 = 1234,
                NullableUnsignedInt32 = 1234565789U,
                NullableUnsignedInt64 = null,
                NullableCharacter = 'a',
                NullableSignedByte = -128,
                NullableGuid = new Guid(guid.ToString()),
                NullableEnumInt32 = null,
                NullableFlagsEnum = FlagsEnum.Default,
                IntArray = new[] {new int?[,] {{1}, {2}}, new int?[,] {{null}, {3}}},
                DecimalArray = new[,] {{new decimal[] {1, 2}, new decimal[] {3, 4}}},
                StringList = new List<string> {"1", null},
                Dictionary = new Dictionary<bool, bool?> {{false, null}, {true, true}},
                Tuple = Tuple.Create(-1, "minus one")
            };

            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.DeserializeAsync<AllSupportedTypes>(script).GetAwaiter().GetResult();

            foreach (var propertyInfo in typeof(AllSupportedTypes).GetProperties())
            {
                Assert.Equal(propertyInfo.GetValue(input, null), propertyInfo.GetValue(output, null));
            }
        }

        public class AllSupportedTypes
        {
            public short Int16 { get; set; }
            public int Int32 { get; set; }
            public long Int64 { get; set; }
            public double Double { get; set; }
            public decimal Decimal { get; set; }
            public DateTime DateTime { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public TimeSpan TimeSpan { get; set; }
            public float Single { get; set; }
            public string String { get; set; }
            public bool Boolean { get; set; }
            public byte Byte { get; set; }
            public ushort UnsignedInt16 { get; set; }
            public uint UnsignedInt32 { get; set; }
            public ulong UnsignedInt64 { get; set; }
            public char Character { get; set; }
            public sbyte SignedByte { get; set; }
            public Guid Guid { get; set; }
            public EnumInt32 EnumInt32 { get; set; }
            public FlagsEnum FlagsEnum { get; set; }
            public short? NullableInt16 { get; set; }
            public int? NullableInt32 { get; set; }
            public long? NullableInt64 { get; set; }
            public double? NullableDouble { get; set; }
            public decimal? NullableDecimal { get; set; }
            public DateTime? NullableDateTime { get; set; }
            public DateTimeOffset? NullableDateTimeOffset { get; set; }
            public TimeSpan? NullableTimeSpan { get; set; }
            public float? NullableSingle { get; set; }
            public bool? NullableBoolean { get; set; }
            public byte? NullableByte { get; set; }
            public Guid? NullableGuid { get; set; }
            public ushort? NullableUnsignedInt16 { get; set; }
            public uint? NullableUnsignedInt32 { get; set; }
            public ulong? NullableUnsignedInt64 { get; set; }
            public char? NullableCharacter { get; set; }
            public sbyte? NullableSignedByte { get; set; }
            public EnumInt32? NullableEnumInt32 { get; set; }
            public FlagsEnum? NullableFlagsEnum { get; set; }
            public int?[][,] IntArray { get; set; }
            public decimal[,][] DecimalArray { get; set; }
            public List<string> StringList { get; set; }
            public Dictionary<bool, bool?> Dictionary { get; set; }
            public Tuple<int, string> Tuple { get; set; }
        }

        public enum EnumInt32
        {
            SomeValue = 1
        }

        [Flags]
        public enum FlagsEnum
        {
            Default = 0,
            FirstFlag = 1 << 0,
            SecondFlag = 1 << 1,
            ThirdFlag = 1 << 2
        }

        [Fact]
        public void NestedGenerics()
        {
            var input = new Nested1<string>.Nested2<int, bool> {Prop = Tuple.Create("1", 1, false)};

            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<Nested1<string>.Nested2<int, bool>>(script);

            Assert.Equal(input.Prop.Item1, output.Prop.Item1);
            Assert.Equal(input.Prop.Item2, output.Prop.Item2);
            Assert.Equal(input.Prop.Item3, output.Prop.Item3);
        }

        [Fact]
        public void NestedRecursiveGenerics()
        {
            var input = new Nested1<string>.Nested3 {Parent = new Nested1<string> {BaseProp = "base"}};

            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<Nested1<string>.Nested3>(script);

            Assert.Equal(input.Parent.BaseProp, output.Parent.BaseProp);
        }

        public class Nested1<T>
        {
            public T BaseProp { get; set; }

            public class Nested2<T1, T2>
            {
                public Tuple<T, T1, T2> Prop { get; set; }
            }

            public class Nested3
            {
                public Nested1<T> Parent { get; set; }
            }
        }

        [Fact]
        public void Serializable()
        {
            var input = new SerializableConstructor("s") {OptionalInt = 1};
            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<SerializableConstructor>(script);

            Assert.Equal(input.RequiredString, output.RequiredString);
            Assert.Equal(input.OptionalInt, output.OptionalInt);
        }

        public class SerializableConstructor : ICSScriptSerializable
        {
            public SerializableConstructor(string requiredString)
            {
                RequiredString = requiredString;
            }

            public string RequiredString { get; }

            public int? OptionalInt { get; set; }

            public ExpressionSyntax GetCreation()
                => new PropertyCSScriptSerializer<SerializableConstructor>(
                    new Dictionary<string, Func<SerializableConstructor, object>>
                    {
                        {nameof(OptionalInt), o => o.OptionalInt}
                    },
                    new List<Func<SerializableConstructor, object>> {o => o.RequiredString})
                    .GetCreation(this);
        }

        [Fact]
        public void CustomSerializerFactory()
        {
            CSScriptSerializer.SerializerFactories.Add(new ConstructorParamsSerializerFactory());

            var input = new ConstructorParams("s") {OptionalInt = 1};
            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<ConstructorParams>(script);

            Assert.Equal(input.RequiredString, output.RequiredString);
            Assert.Equal(input.OptionalInt, output.OptionalInt);

            CSScriptSerializer.SerializerFactories.RemoveAt(0);
        }

        [Fact]
        public void CustomSerializer()
        {
            ICSScriptSerializer _;
            CSScriptSerializer.Serializers.TryRemove(typeof(ConstructorParams), out _);
            CSScriptSerializer.Serializers[typeof(ConstructorParams)] =
                new PropertyCSScriptSerializer<ConstructorParams>(
                    new Dictionary<string, Func<ConstructorParams, object>>
                    {
                        {nameof(ConstructorParams.OptionalInt), o => o.OptionalInt}
                    },
                    new List<Func<ConstructorParams, object>> {o => o.RequiredString});

            var input = new ConstructorParams("s") {OptionalInt = 1};
            var script = CSScriptSerializer.Serialize(input);
            var output = CSScriptSerializer.Deserialize<ConstructorParams>(script);

            Assert.Equal(input.RequiredString, output.RequiredString);
            Assert.Equal(input.OptionalInt, output.OptionalInt);

            CSScriptSerializer.Serializers.TryRemove(typeof(ConstructorParams), out _);
        }

        public class ConstructorParams
        {
            public ConstructorParams(string requiredString)
            {
                RequiredString = requiredString;
            }

            public string RequiredString { get; }

            public int? OptionalInt { get; set; }
        }

        public class ConstructorParamsSerializerFactory : ICSScriptSerializerFactory
        {
            public ICSScriptSerializer TryCreate(Type type)
                => type == typeof(ConstructorParams)
                    ? new PropertyCSScriptSerializer<ConstructorParams>(
                        new Dictionary<string, Func<ConstructorParams, object>>
                        {
                            {nameof(ConstructorParams.OptionalInt), o => o.OptionalInt}
                        },
                        new List<Func<ConstructorParams, object>> {o => o.RequiredString})
                    : null;
        }
    }
}