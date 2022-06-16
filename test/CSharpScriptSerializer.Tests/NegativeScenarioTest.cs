using System;
using System.Collections.Generic;
using Xunit;

namespace CSharpScriptSerialization.Tests
{
    public class NegativeScenarioTest
    {
        [Fact]
        public void NoUsableProperties()
        {
            var input = new Private();

            Assert.Equal($"The type {typeof(Private)} does not have public writable properties",
                Assert.Throws<InvalidOperationException>(() => CSScriptSerializer.Serialize(input)).Message);
        }

        [Fact]
        public void UnusablePropertySpecified()
        {
            Assert.Equal($"The type {typeof(Private)} does not have a public nonstatic writable property PrivateProperty",
                Assert.Throws<InvalidOperationException>(() => new PropertyCSScriptSerializer<Private>(
                    new Dictionary<string, Func<Private, object, bool>>
                    {
                        {"PrivateProperty", (o, v) => v != null}
                    })).Message);
        }

        public class Private
        {
            private int PrivateProperty { get; set; }
        }
    }
}