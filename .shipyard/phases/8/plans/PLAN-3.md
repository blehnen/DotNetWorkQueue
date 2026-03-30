# Build & Config Fixes Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Fix build configuration issues, clean up stale files, and regenerate XML documentation.

**Architecture:** Changes to `.csproj`, `.gitignore`, and generated `.xml` files. Deletes stale artifacts. No code logic changes.

**Tech Stack:** MSBuild / .NET CLI

**Dependencies:** Run after PLAN-2 (Code Fixes) so XML documentation regeneration reflects the final code state.

---

## Task 1: Delete xunit.runner.json (M-4)

**Files:**
- Delete: `Source/xunit.runner.json`

**Step 1: Verify the file exists and confirm it's xUnit config**

```bash
cat Source/xunit.runner.json
```

**Step 2: Delete the file**

```bash
rm Source/xunit.runner.json
```

**Verification:**

```bash
test -f Source/xunit.runner.json && echo "STILL EXISTS" || echo "DELETED"
# Expected: DELETED
```

---

## Task 2: Fix Malformed DocumentationFile Path in SQLite .csproj (M-5)

**Files:**
- Modify: `Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj` (lines 45, 52, 59)

**Step 1: Fix the three malformed lines**

Lines 45, 52, and 59 contain:
```xml
<DocumentationFile>&gt;DotNetWorkQueue.Transport.SQLite.xml</DocumentationFile>
```

Which decodes to `>DotNetWorkQueue.Transport.SQLite.xml` (note the leading `>`).

Change each to:
```xml
<DocumentationFile>DotNetWorkQueue.Transport.SQLite.xml</DocumentationFile>
```

Lines 40 and 67 are already correct — do not modify those.

**Verification:**

```bash
grep "DocumentationFile" Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj
# Expected: All 5 lines show just "DotNetWorkQueue.Transport.SQLite.xml" without leading ">"
grep "&gt;" Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj
# Expected: 0 matches
```

---

## Task 3: Add Stale File Patterns to .gitignore and Clean Up (M-8)

**Files:**
- Modify: `.gitignore`
- Delete: `Source/Source.7z`
- Delete: `TeamCity_DotNetWorkQueueGitCore_20260324_130127.zip`
- Delete: `Source/DotNetWorkQueue.sln.DotSettings.user`

**Step 1: Add patterns to .gitignore**

Add a new section at the end of `.gitignore`:

```
# Stale artifacts and working files (M-8)
*.7z
TeamCity_*.zip
codcov*.txt
codecov*.txt
*.DotSettings.user
```

**Step 2: Delete the stale files from working tree**

```bash
rm -f Source/Source.7z
rm -f TeamCity_DotNetWorkQueueGitCore_20260324_130127.zip
rm -f Source/DotNetWorkQueue.sln.DotSettings.user
```

**Note:** `codcov*.txt` files were not found on disk (already deleted or never tracked). The `.gitignore` patterns prevent future occurrences.

**Verification:**

```bash
test -f Source/Source.7z && echo "STILL EXISTS" || echo "DELETED"
# Expected: DELETED
test -f TeamCity_DotNetWorkQueueGitCore_20260324_130127.zip && echo "STILL EXISTS" || echo "DELETED"
# Expected: DELETED
grep "DotSettings.user" .gitignore
# Expected: *.DotSettings.user pattern present
```

---

## Task 4: Regenerate XML Documentation (N-4)

**Files:**
- Modify: `Source/DotNetWorkQueue/DotNetWorkQueue.xml` (auto-generated)

**Step 1: Verify stale references exist**

```bash
grep -c "AbortWorkerThread" Source/DotNetWorkQueue/DotNetWorkQueue.xml
# Expected: >0 (confirms stale references)
```

**Step 2: Rebuild the core project in Release mode**

Release mode enables `GenerateDocumentationFile`, which regenerates the XML.

```bash
dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Release
```

If this fails due to warnings (TreatWarningsAsErrors), ensure M-5 fix (DocumentationFile path) has been applied and try:

```bash
dotnet build Source/DotNetWorkQueue/DotNetWorkQueue.csproj -c Release -f net10.0
```

**Step 3: Verify stale references are gone**

```bash
grep -c "AbortWorkerThread" Source/DotNetWorkQueue/DotNetWorkQueue.xml
# Expected: 0
```

**Verification:**

```bash
grep "AbortWorkerThread\|IAbortWorkerThread\|AbortWorkerThreadDecorator\|AbortWorkerThreadsWhenStopping" Source/DotNetWorkQueue/DotNetWorkQueue.xml
# Expected: 0 matches
```

**Commit:**

```bash
git add Source/xunit.runner.json Source/DotNetWorkQueue.Transport.SQLite/DotNetWorkQueue.Transport.SQLite.csproj .gitignore Source/DotNetWorkQueue/DotNetWorkQueue.xml
git add -u Source/Source.7z TeamCity_DotNetWorkQueueGitCore_20260324_130127.zip Source/DotNetWorkQueue.sln.DotSettings.user
git commit -m "fix: build config cleanup — M-4, M-5, M-8, N-4"
```

---

## Post-Plan: Update CONCERNS.md and ISSUES.md

After all 3 plans complete, update the tracking documents:

1. In `.shipyard/codebase/CONCERNS.md`:
   - Mark M-4, M-5, M-8, L-5, N-4 as `[Resolved - 2026-03-30]`
   - Update summary table

2. In `.shipyard/ISSUES.md`:
   - Move all 13 issues from Open to Closed with resolution date and brief note
   - For ISSUE-001 and ISSUE-002: note "Already resolved" if verified during PLAN-2

3. Final commit:
```bash
git add .shipyard/codebase/CONCERNS.md .shipyard/ISSUES.md
git commit -m "docs: update CONCERNS.md and ISSUES.md — mark all tier A items resolved"
```
