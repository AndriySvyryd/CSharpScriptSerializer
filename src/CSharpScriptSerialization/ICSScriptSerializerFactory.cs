using System;

namespace CSharpScriptSerialization
{
    public interface ICSScriptSerializerFactory
    {
        ICSScriptSerializer TryCreate(Type type);
    }
}