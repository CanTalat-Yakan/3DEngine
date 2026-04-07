// Polyfill for 'init' accessors and 'record' types when targeting netstandard2.0.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>Polyfill enabling <c>init</c> accessors when targeting netstandard2.0.</summary>
    internal static class IsExternalInit { }
}
