# Phase 1 Context: Core Library, Transport Libraries, and Vendored DLL Cleanup

## Decisions

- **Branch:** Feature branch `issue-101-drop-net48` off master
- **Skip research:** ROADMAP.md already exhaustive — grep analysis done during brainstorming (186 occurrences, 127 files)
- **Scope:** Exactly as described in ROADMAP.md Phase 1 — core csproj, 10 .cs files, 8 transport csproj, vendored DLL deletion
- **CompileException.cs:** Needs verification during build — keep class if referenced outside `#if NETFULL`, remove only NETFULL-specific members
