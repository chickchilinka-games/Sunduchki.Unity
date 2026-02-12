using System;

namespace Modules.AssetSystem.Exceptions
{
    public class AssetNotFoundException : Exception
    {
        public AssetNotFoundException(string id, Type type) : base($"Asset {id} of type {type.FullName} not found")
        {
        }
    }
}
