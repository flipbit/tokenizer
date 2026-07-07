#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure — polyfill intentionally uses system namespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved for compiler use. This type enables the use of init accessors in netstandard2.0.
/// </summary>
internal static class IsExternalInit;
#pragma warning restore IDE0130
#endif
