// Polyfill required for C# 9 'init' setters on targets below net5.0 (e.g. netstandard2.0, net48).
// The compiler emits a reference to System.Runtime.CompilerServices.IsExternalInit; this stub
// satisfies that reference without any runtime overhead.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
