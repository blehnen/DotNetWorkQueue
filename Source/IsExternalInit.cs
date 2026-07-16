// ---------------------------------------------------------------------
// SPIKE-ONLY POLYFILL (issue #165) — NOT part of the 0.6.6 product tree.
//
// #164's code uses C# 9 init-only setters. The compiler lowers `init` accessors to a
// modreq on System.Runtime.CompilerServices.IsExternalInit, which only ships in the
// runtime from net5.0 onward. On net461/net472/net48/netstandard2.0 the base build
// therefore fails with:
//     CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not
//             defined or imported
//
// Declaring the type internally is the standard, Microsoft-sanctioned workaround. It is
// compile-time only — it emits no runtime dependency and changes no public API surface.
//
// This file is linked into the old-TFM builds by Source/Directory.Build.targets.
//
// ⚠️ If a real 0.6.6.1 is produced, this polyfill (or dropping `init` from the ported
// types) is a PREREQUISITE and must be disclosed. See phases/3/CONTEXT-3.md decision F4.
// ---------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved for use by the compiler to support init-only setters on target frameworks
    /// whose runtime does not supply this type (net461/net472/net48/netstandard2.0).
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
