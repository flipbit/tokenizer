#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
#pragma warning disable MA0048 // File name must match type name — polyfill for System.Diagnostics.CodeAnalysis.NotNullWhenAttribute
#pragma warning disable IDE0130 // Namespace does not match folder structure — polyfill intentionally uses system namespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that when a method returns <see cref="ReturnValue"/>,
/// the parameter will not be null even if the corresponding type allows it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
    public bool ReturnValue { get; }
}
#pragma warning restore MA0048
#pragma warning restore IDE0130
#endif
