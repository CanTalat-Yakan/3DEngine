// Polyfill required for 'init' accessors and 'record' types when targeting netstandard2.0.
// The compiler emits references to this type, but it only exists in .NET 5+.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

