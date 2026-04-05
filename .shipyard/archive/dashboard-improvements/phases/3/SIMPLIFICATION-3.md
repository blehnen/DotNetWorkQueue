# Simplification Report
**Phase:** 3 — Docker Image for Dashboard
**Date:** 2026-04-02
**Files analyzed:** 5
**Findings:** 0 High, 2 Medium, 3 Low

---

## Medium Priority

### Duplicated libdl.so symlink block across both Dockerfiles
- **Type:** Consolidate
- **Locations:** `docker/Dockerfile:24-27`, `docker/dashboard/Dockerfile:48-51`
- **Description:** The `libdl.so` symlink creation RUN block is character-for-character identical in both files — same conditional, same two fallback `ln -s` targets, same `2>/dev/null` suppression. It exists because both images need SQLite/LiteDB support. If the path or logic ever needs updating (e.g., for ARM64 support, which the security audit flagged as broken on non-x86), the fix must be applied in two places.
- **Suggestion:** Extract a shared base image (`docker/base/Dockerfile`) that installs `libsqlite3-0` and creates the symlink, then have both consuming Dockerfiles use it as their `FROM`. Alternatively, at minimum add a comment in each file cross-referencing the other so the duplication is intentional and tracked. The base-image approach is the cleaner path if a third consumer ever appears.
- **Impact:** Eliminates one copy of an 4-line RUN block; more importantly removes the silent divergence risk if one copy is updated and the other is not.

### Duplicated .NET 8 SDK installation block across both Dockerfiles
- **Type:** Consolidate
- **Locations:** `docker/Dockerfile:4-9`, `docker/dashboard/Dockerfile:4-10`
- **Description:** The multi-line `RUN` that downloads, extracts, and removes the .NET 8 SDK tarball is nearly identical in both files. The only structural difference is that `docker/Dockerfile` is a build-only image while `docker/dashboard/Dockerfile` uses a multi-stage build — but the SDK installation step in the build stage is the same. Both pin to version `8.0.408` and use the same CDN URL pattern.
- **Suggestion:** Same resolution as above — a shared build-stage base image eliminates both duplicates simultaneously. If a base image is not desired, add a comment in each file naming the version variable and the other file that must be updated in sync. The security audit already flagged adding checksum verification here; a single shared location makes that fix a one-time change.
- **Impact:** Single version-pin to update when upgrading the .NET 8 SDK, single location to add integrity verification.

---

## Low Priority

- **`DashboardApi:BaseUrl` defaults to port 5000 in self-contained mode** (`Program.cs:45`). In self-contained mode the app and API share one process on port 8080, but if `DashboardApi:BaseUrl` is not overridden in config it falls back to `http://localhost:5000`. A user who copies the example config (which correctly sets `BaseUrl` to port 8080) is fine, but a user who omits the section entirely will get silent HTTP failures in self-contained mode. A one-line comment at `Program.cs:45` noting that self-contained mode requires this to match `ASPNETCORE_URLS` would remove the confusion.

- **`selfContained` is evaluated at startup and gates three separate code blocks** (`Program.cs:38-42`, `93-96`, `104-107`). The variable is a `bool` local set once and referenced three times across the file. This is clean and not itself a problem, but the middle block (`app.UseDotNetWorkQueueDashboard()` at line 93) is separated from its paired `builder.Services.AddDotNetWorkQueueDashboard()` at line 41 by 50+ lines of unrelated setup. A brief comment at line 93 noting "paired with AddDotNetWorkQueueDashboard above" would make the relationship immediately visible without requiring a reader to remember the earlier conditional.

- **`appsettings.example.json` includes all five transports simultaneously** (`docker/dashboard/appsettings.example.json:21-51`). This is correct behavior for a comprehensive example, but a user copying it verbatim gets a config that tries to reach five different backend services — four of which will fail immediately in any typical deployment. A comment block at the top of the `Connections` array noting "remove any transports you are not using" would reduce first-run confusion at zero code cost.

---

## Summary

- **Duplication found:** 2 identical or near-identical RUN blocks across 2 Dockerfiles
- **Dead code found:** 0
- **Complexity hotspots:** 0 (Program.cs is 142 lines, linear top-to-bottom, no nested branches exceeding 2 levels)
- **AI bloat patterns:** 0 (error handling is appropriate, no redundant type checks, no re-raising wrappers)
- **Estimated cleanup impact:** ~8 lines removable via shared base image; 3 clarifying comments addable with no line cost

## Recommendation

**Defer.** The Phase 3 additions are clean. Program.cs is compact and linear for what it accomplishes. The appsettings example is well-structured. The two medium findings (duplicated Dockerfile blocks) are real and worth addressing, but only become painful when one copy is updated and the other is not — which has not happened yet. Track them and act when a third consumer appears or when the security audit's checksum-verification fix is implemented (at that point, fixing in one shared location is cheaper than fixing in two).

The low-priority items are all comment additions; any builder can handle them as part of the next touchup pass on these files.
