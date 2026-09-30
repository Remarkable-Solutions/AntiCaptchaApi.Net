#if !NET5_0_OR_GREATER
// Polyfills for the nullable static-analysis attributes missing from netstandard2.0.
// The compiler recognises them by name, so internal copies are enough.
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
internal sealed class AllowNullAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
internal sealed class DisallowNullAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class MaybeNullAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, Inherited = false)]
internal sealed class NotNullAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    public MaybeNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    public NotNullIfNotNullAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal sealed class DoesNotReturnAttribute : Attribute
{
}
#endif
