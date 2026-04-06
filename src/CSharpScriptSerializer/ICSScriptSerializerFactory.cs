using System;

namespace CSharpScriptSerialization
{
    /// <summary>
    ///     A factory that can create an <see cref="ICSScriptSerializer"/> for a given <see cref="Type"/>.
    ///     Register instances in <see cref="CSScriptSerializer.SerializerFactories"/> to support custom types.
    /// </summary>
    public interface ICSScriptSerializerFactory
    {
        /// <summary>
        ///     Returns an <see cref="ICSScriptSerializer"/> for <paramref name="type"/>,
        ///     or <see langword="null"/> if this factory does not handle that type.
        /// </summary>
        /// <param name="type">The type to create a serializer for.</param>
        ICSScriptSerializer TryCreate(Type type);
    }
}