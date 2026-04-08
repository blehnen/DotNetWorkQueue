# Simplification Review: Phase 1

## Priority: LOW — Ship as-is

## Findings

### Potential Dead Code (Low Priority)
1. **CompileException.cs** — Only thrown by `LinqCompiler.CompileAction()`, which now throws `NotSupportedException` instead. `CompileException` is no longer reachable at runtime. However, it's a public type and removing it would be a separate breaking change. **Defer** to a future cleanup.

2. **LinqExpressionToRun.cs** — No public API accepts this type after removing the `#if NETFULL` methods. Kept intentionally for serialization compatibility (existing queued messages may contain this type). **Keep**.

3. **ILinqCompiler + decorators** — The interface and its decorator chain (LinqCompileCacheDecorator, LinqCompilerDecorator) still exist but the implementation throws NotSupportedException. This is correct — removing the DI registration would break container resolution. **Keep**.

### csproj PropertyGroup Duplication (Informational)
- net8.0 and net10.0 Debug/Release PropertyGroups have nearly identical content across all 9 library csproj files. This is pre-existing — not introduced by this phase. **Out of scope**.

## Recommendation

Ship as-is. The potential dead code (CompileException) is a minor concern that doesn't warrant a fix in this phase. The serialization types are intentionally preserved.
