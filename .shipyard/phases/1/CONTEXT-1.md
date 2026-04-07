# Phase 1 Context: Prepare Fork for NuGet Publishing

## Decisions

- **License:** MIT (matches original aquilae repo)
- **Jenkins TFMs:** net10.0 + net8.0 only (Docker agent lacks net48 targeting pack)
- **GitHub Actions:** Matrix build — ubuntu for net10.0/net8.0/netstandard2.0, windows-latest for net48
- **Upstream merge:** Merge aquilae/expression-json-serializer master (loop/goto support) before other changes
- **Skip research:** Fork structure already explored during brainstorming — csproj, source files, test project all verified
