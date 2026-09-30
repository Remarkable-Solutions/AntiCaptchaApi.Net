#if !NET5_0_OR_GREATER
// Polyfill so that records and init-only setters compile on netstandard2.0.
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}
#endif
