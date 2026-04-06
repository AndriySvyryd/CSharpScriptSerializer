namespace CSharpScriptSerialization
{
    /// <summary>
    ///     Allows a type to supply its own <see cref="ICSScriptSerializer"/> instead of relying on
    ///     the default serializer discovery logic in <see cref="CSScriptSerializer"/>.
    /// </summary>
    public interface ICSScriptSerializable
    {
        /// <summary>
        ///     Returns the <see cref="ICSScriptSerializer"/> to use when serializing this object.
        /// </summary>
        ICSScriptSerializer GetSerializer();
    }
}