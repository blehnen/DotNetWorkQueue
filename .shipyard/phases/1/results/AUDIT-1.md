# Security Audit Report — Phase 1

## Executive Summary

**Verdict:** PASS
**Risk Level:** Low

This phase prepares the Aq.ExpressionJsonSerializer fork for NuGet publishing: it adds package metadata to the csproj, a GitHub Actions CI/publish workflow, and a Jenkinsfile for internal CI. No secrets were committed to version control. The NuGet API key is handled correctly via a repository secret. The upstream-merged loop/goto expression support follows the same safe reflection pattern as the rest of the library and introduces no new attack surface. All findings are advisories around CI supply-chain hygiene (action pinning, missing workflow permissions declaration) that are common in new workflow files and carry no exploitable risk in this repository's context. This phase may proceed to ship.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | No workflow-level `permissions` block declared | `.github/workflows/ci.yml` | Trivial | Add `permissions: contents: read` at workflow level to enforce least privilege |
| 2 | Actions pinned to mutable tags, not commit SHAs | `.github/workflows/ci.yml:30,56` | Small | Pin `actions/checkout` and `actions/setup-dotnet` to full commit SHAs |
| 3 | `--skip-duplicate` silently swallows re-publish | `.github/workflows/ci.yml:77,81` | Trivial | Decide whether a 409 on re-publish should be a hard failure; remove flag if so |
| 4 | Jenkinsfile has no timeout or build-discard options | `Jenkinsfile` | Trivial | Add `options { timeout(time:30, unit:'MINUTES'); buildDiscarder(logRotator(numToKeepStr:'10')) }` |

### Themes
- CI hardening gaps common in new workflow files: missing least-privilege `permissions` and mutable action tag references.
- Pre-existing deserialization design (assembly-scoped reflection) is an accepted risk documented in the project's security model — not introduced by this phase.

## Detailed Findings

### Critical

None.

### Important

None.

### Advisory

- **No workflow-level `permissions` declaration** (`.github/workflows/ci.yml`, entire file) — GitHub Actions defaults to broad permissions when no `permissions:` key is set. For a build/test/publish workflow, `permissions: contents: read` at the top level (plus `id-token: write` on the publish job if OIDC is ever adopted) is sufficient and enforces least privilege. Reference: GitHub Actions security hardening guide, OWASP CI/CD Security Top 10 #5.

- **Third-party actions referenced by mutable tag, not commit SHA** (`.github/workflows/ci.yml:30, 34, 56, 61`) — `actions/checkout@v4` and `actions/setup-dotnet@v4` are mutable tag references. A tag can be moved by the upstream maintainer or a compromised account. Pin each to its current commit SHA (e.g., `actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683` for v4.2.2) to lock the dependency. Reference: SLSA Level 2, CWE-829.

- **`--skip-duplicate` on NuGet push suppresses 409 errors** (`.github/workflows/ci.yml:77,81`) — If a tag is re-pushed or the version is not incremented, the push silently reports success from CI even though nothing was published. This can mask version-management mistakes. If re-publishing an existing version should be a build error, remove the flag.

- **Jenkinsfile has no `timeout` or `buildDiscarder` option** (`Jenkinsfile`, no `options {}` block) — Without a timeout, a hung `dotnet test` process holds a Jenkins executor indefinitely. Without `buildDiscarder`, workspace and log history accumulate without bound. This is an operational concern, not a security vulnerability, but it can enable a resource exhaustion condition on the Jenkins agent.

## Cross-Component Analysis

**Publish gate integrity:** The publish job is correctly gated behind `needs: build-and-test` and `if: startsWith(github.ref, 'refs/tags/v')`. A tag push cannot bypass the build-and-test matrix. The `NUGET_API_KEY` is consumed only via `${{ secrets.NUGET_API_KEY }}` and is never echoed, interpolated into a string that could appear in logs, or assigned to an environment variable. The secret handling pattern is correct.

**Jenkinsfile scope vs. GitHub Actions matrix:** The Jenkinsfile runs only net10.0 and net8.0 — it omits net48. GitHub Actions covers all three TFMs, with net48 running on `windows-latest`. This is intentional (Jenkins agents are Linux Docker containers) and consistent with the main project's CI architecture. No security gap is created by the omission.

**Upstream loop/goto merge and deserialization safety:** The `Deserializer.cs` dispatch table (line 81–82) adds `goto` and `loop` cases. Both ultimately call `CreateLabelTarget` (lines 103–110), which constructs `LabelTarget` objects via the standard `System.Linq.Expressions.Expression.Label` factory — no arbitrary type loading beyond what the rest of the library already performs. The `assembly` parameter passed at entry is the caller's own assembly (see `ExpressionJsonSerializerTest.cs:272`), bounding reflection to types the calling application already trusts. No new attack surface is introduced. This is consistent with the accepted CWE-502 / OWASP A08 design risk recorded in the project security model memory.

**No secrets in any committed file:** All six changed files were inspected. No API keys, tokens, passwords, connection strings, private keys, or base64-encoded credentials are present anywhere.

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | Deserializer.cs loop/goto additions reviewed; no new injection or unsafe deserialization surface |
| Secrets & Credentials | Yes | All 6 changed files scanned; no secrets found |
| Dependencies | Yes | See Dependency Status table below |
| Infrastructure as Code | N/A | No Terraform or Ansible files changed |
| Docker/Container | N/A | No Dockerfile changed |
| Configuration | Yes | csproj NuGet metadata and CI workflow reviewed |

## Dependency Status

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| Newtonsoft.Json | 13.0.4 | None at audit date (2026-04-07) | OK |
| Microsoft.SourceLink.GitHub | 8.0.0 | None at audit date | OK |
| Microsoft.NET.Test.Sdk | 17.12.0 | None at audit date | OK |
| xunit | 2.9.3 | None at audit date | OK |
| xunit.runner.visualstudio | 2.9.3 | None at audit date | OK |
| xunit.runner.console | 2.9.3 | None at audit date | OK |

All versions are pinned to exact version numbers (no floating ranges). No `packages.lock.json` is committed; this is consistent with the upstream project's conventions and is not a regression introduced by this phase.

## IaC Findings

Not applicable — no Terraform, Ansible, or Dockerfile changes in this phase.
