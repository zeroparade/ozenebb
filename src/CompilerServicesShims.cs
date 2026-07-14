using System;

// Some generated IL2CPP interop assemblies expose nullable attributes without
// the constructors Roslyn requires. Local internal definitions keep builds
// reproducible without becoming part of the mod's public API.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field |
                    AttributeTargets.GenericParameter | AttributeTargets.Module |
                    AttributeTargets.Parameter | AttributeTargets.Property |
                    AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NullableAttribute : Attribute
    {
        internal NullableAttribute(byte flag)
        {
            NullableFlags = new[] { flag };
        }

        internal NullableAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }

        internal byte[] NullableFlags { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate |
                    AttributeTargets.Interface | AttributeTargets.Method |
                    AttributeTargets.Module | AttributeTargets.Struct,
                    Inherited = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        internal NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }

        internal byte Flag { get; }
    }
}
