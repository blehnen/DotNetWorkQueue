# Review: Plan 2.2 (Phase 3 Wave 2 — Async Fork)

## Verdict: PASS

Stage 1 spec compliance clean. Stage 2 findings minor and non-blocking. Builder's
comment-rephrase deviation is sound and the cross-plan inconsistency with PLAN-2.1
is cosmetic only (both files now carry semantically equivalent lifecycle-ownership
comments; both fork bodies are byte-equivalent to the spec).

## Stage 1: Spec Compliance

### Task 1: HandleExternalTxAsync + early-branch dispatch — PASS
- **Early-branch placement:** `SendMessageCommandHandlerAsync.cs:107-108` — placed immediately
  after the lazy-init `_messageExpirationEnabled` block (line 102-105) and before
  `var jobName = ...` (line 110), exactly as the plan specifies.
- **Branch form:** `if (commandSend.ExternalTransaction != null) return await HandleExternalTxAsync(commandSend).ConfigureAwait(false);` —
  matches plan literally including `.ConfigureAwait(false)`.
- **Method signature:** `private async Task<long> HandleExternalTxAsync(SendMessageCommand commandSend)`
  at line 203. Return type, accessibility, and parameter list match spec.
- **Async I/O calls:** `await command.ExecuteScalarAsync().ConfigureAwait(false)` (line 249);
  `await CreateMetaDataRecordAsync(...).ConfigureAwait(false)` (line 264-265);
  `await CreateStatusRecordAsync(...).ConfigureAwait(false)` (line 269-270). All three
  awaits use `.ConfigureAwait(false)` consistent with the existing handler's pattern (e.g.
  the self-managed path's line 147 `await command.ExecuteScalarAsync().ConfigureAwait(false)`).
- **Sync sub-handler calls preserved:** `_jobExistsHandler.Handle(...)` (line 222) and
  `_sendJobStatus.Handle(...)` (line 275) are synchronous — matches plan §3 and the
  self-managed path's sync invocations at lines 124 + 169.
- **Helper reuse:** `CreateMetaDataRecordAsync` (line 316) and `CreateStatusRecordAsync`
  (line 292) are reused verbatim — no signature changes.
- **Lifecycle invariant:** Fork body (lines 203-281) contains zero `.Commit()`,
  `.Rollback()`, `.Close()`, `.Dispose()`, `.CommitAsync`, `.RollbackAsync`,
  `.CloseAsync`, or `.DisposeAsync` calls (confirmed by smoke test + visual review).
- **Self-managed path unchanged:** Lines 110-187 are identical to the pre-Phase-3
  baseline (the `using (var connection = new SqlConnection(...))` block + `trans.Commit()`
  at line 179).

### Task 2: Structural smoke tests — PASS
- **File created:** `Source/DotNetWorkQueue.Transport.SqlServer.Tests/Basic/CommandHandler/SendMessageCommandHandlerAsyncForkSmokeTests.cs`
  with LGPL-2.1 header (lines 1-18) matching repo convention.
- **Test 1 (`HandleExternalTxAsync_PrivateMethod_ExistsWithExpectedSignature`):** Reflection-
  resolves the private instance method, asserts not-null AND `Task<long>` return type.
- **Test 2 (`HandleAsync_SourceContainsExternalTransactionEarlyBranch`):** Asserts four
  source substrings — the null-check, the dispatch call, the method declaration, and
  the awaited-with-`ConfigureAwait(false)` form. Source path resolution uses
  `Assembly.Location` + relative climb; sane.
- **Test 3 (`HandleExternalTxAsync_DoesNotCommitOrRollbackOrCloseOrDispose`):** Eight
  `Assert.IsFalse(Contains)` guards (4 sync + 4 async lifecycle calls), substringing
  the 6500-char window starting at `private async Task<long> HandleExternalTxAsync`.
  Async-specific variants (`.CommitAsync`, `.RollbackAsync`, `.CloseAsync`, `.DisposeAsync`)
  correctly added per plan.
- **Verification per SUMMARY:** 3 passed; 156 SqlServer.Tests pass; Release build clean.

### Verification gates per plan §Verification — PASS
| Gate | Spec | SUMMARY observed |
|---|---|---|
| `commandSend.ExternalTransaction != null` grep | 1 match | 1 match |
| `private async Task<long> HandleExternalTxAsync` grep | 1 match | 1 match |
| `await HandleExternalTxAsync(commandSend).ConfigureAwait(false)` grep | 1 match | 1 match |
| Fork body lifecycle-call grep | 0 matches | 0 matches |
| Release build of `Transport.SqlServer` | 0 errors | 0 errors |
| 3 async fork smoke tests | 3 passed | 3 passed |
| Full SqlServer.Tests suite | Failed: 0 | 156 passed |

## Stage 2: Code Quality

### Critical
- None.

### Important
- None.

### Minor
1. **Comment wording divergence between sync and async handler files** (cross-plan
   cosmetic inconsistency).
   - `SendMessageCommandHandler.cs:276-277` carries the original verbose form: `// Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close(). // The caller owns the transaction lifecycle.`
   - `SendMessageCommandHandlerAsync.cs:279` carries the rephrased single-line form: `// Caller owns lifecycle: no Commit, Rollback, Close, or Dispose performed here.`
   - Both convey the same lifecycle-ownership invariant; both are semantically correct.
     The divergence is cosmetic but visible to future readers diff-comparing the sync vs
     async forks side-by-side.
   - Remediation (optional, not blocking): normalize both files to the same wording in a
     future cleanup pass. The async form is slightly more concise and avoids the smoke-
     test false positive that PLAN-2.1 chose to handle test-side; standardizing on the
     async wording would also let PLAN-2.1's test-side line-comment stripping be
     simplified or dropped. Defer to Phase 3 polish or a follow-up cleanup commit.

2. **Test 3 substring window is fixed at 6500 chars** (smoke-test brittleness, identical
   to PLAN-2.1's pattern).
   - `SendMessageCommandHandlerAsyncForkSmokeTests.cs:89` — `forkBody = content.Substring(forkStart, System.Math.Min(6500, content.Length - forkStart))`.
   - The current fork body is ~80 lines / ~3.5 KB so 6500 is comfortable. If
     a future edit pushes the fork past 6500 chars (e.g., a sub-handler call needing
     additional setup), the substring will silently truncate, the guard's downstream
     `Contains` checks will then scan a partial fork and any newly-added lifecycle
     call past offset 6500 would be missed.
   - Remediation: substring to the next `private` declaration (delimiter-bounded) or
     a sentinel like `// Caller owns lifecycle:`. Not blocking — same pattern in
     PLAN-2.1 already accepted by REVIEW-2.1's reviewer (implicitly).

3. **Test 2 absence of `Assert.IsTrue(File.Exists(...))` after the second source-path
   construction** (test brittleness, minor).
   - `SendMessageCommandHandlerAsyncForkSmokeTests.cs:77-83` (Test 3) computes
     `sourcePath` and calls `File.ReadAllText(sourcePath)` without a prior `File.Exists`
     guard. Test 2 (line 62) does guard. If the relative path resolution ever drifts
     (e.g., a future test-runner change to assembly location), Test 3 would throw
     `FileNotFoundException` with a less helpful stack trace than Test 2's explicit
     "Expected source at {path} not found" message.
   - Remediation (optional): copy the `Assert.IsTrue(File.Exists(...))` guard into Test 3
     mirror of Test 2.

### Positive
- LGPL-2.1 header on new test file (lines 1-18) matches repo convention exactly.
- XML doc on `HandleExternalTxAsync` (lines 189-202) is thorough — covers method intent,
  the lifecycle invariant, the producer-side validation context, and exceptions.
- `.ConfigureAwait(false)` discipline maintained on every await inside the fork (3 calls
  total: `ExecuteScalarAsync`, `CreateMetaDataRecordAsync`, `CreateStatusRecordAsync`).
- No `Microsoft.Data.SqlClient.SqlConnection`/`SqlTransaction` cast leaks into a sub-
  handler invocation — the casts are local to the fork and the sub-handlers accept
  `SqlConnection`/`SqlTransaction` typed parameters consistent with the existing
  `DoesJobExistQuery<SqlConnection, SqlTransaction>` + `SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>`
  type arguments — no abstraction-layering regression.
- Self-managed path (lines 110-187) preserved byte-equivalent to the pre-Phase-3
  baseline — `trans.Commit()` at line 179 still fires for the non-external-tx path.
- Reuse of existing private helpers (`CreateMetaDataRecordAsync`, `CreateStatusRecordAsync`)
  avoids SQL-builder duplication and keeps the fork's diff minimal.
- Async-specific lifecycle-call guards in the smoke test (`.CommitAsync`, `.RollbackAsync`,
  `.CloseAsync`, `.DisposeAsync`) — correct catch for the async-specific risk surface
  PLAN-2.1's sync-only guard would miss.

## Deviation audit

### Comment rephrase
- **Sound:** YES. The rephrased comment `// Caller owns lifecycle: no Commit, Rollback,
  Close, or Dispose performed here.` conveys the lifecycle-ownership invariant clearly
  and preserves the named-method enumeration (Commit, Rollback, Close, Dispose) so a
  future reader can still trace which methods the invariant forbids. The absence of
  `()` after the method names does not materially weaken the documentation — the
  comment's role is to declare intent, not pin a literal API surface (which the smoke
  test does anyway). The lifecycle invariant is the load-bearing contract; the smoke
  test enforces it programmatically; the comment is documentary.

### Cross-plan inconsistency note
- `SendMessageCommandHandler.cs:276-277` (PLAN-2.1) and `SendMessageCommandHandlerAsync.cs:279`
  (PLAN-2.2) now carry **different wording** for the same lifecycle-ownership comment.
  Both PLAN-2.1's SUMMARY-2.1 §Decisions Made and PLAN-2.2's SUMMARY-2.2 §Decisions Made
  document the divergence explicitly. The divergence is **cosmetic only**:
  - Both fork bodies are byte-equivalent to the plan's code block (the comment is the
    only difference between the two solutions).
  - Both files pass identical lifecycle-ownership smoke tests (PLAN-2.1's test strips
    line-comments before checking; PLAN-2.2's source avoids the substring entirely).
  - The architectural invariant — "no Commit/Rollback/Close/Dispose on caller's
    resources" — is identically enforced in both files at the source level.
- The reviewer's recommendation is to normalize the comment wording across both files in
  a future cleanup, but this is not blocking and is recorded as Minor #1 above.

## Tally
Critical: 0 | Important: 0 | Minor: 3 | Positive: 6+

## Wave-3 Hand-off Notes
- Both Wave-2 forks are now live; the SqlServer outbox path is end-to-end functional
  at the unit-test level on both sync and async dispatch.
- Phase 6 integration tests against a real SqlServer will exercise:
  - Atomic-commit semantics on caller-tx (PROJECT.md §Success Criteria #6).
  - Atomic-rollback semantics on caller-tx (PROJECT.md §Success Criteria #6).
  - Concurrent-send single-threaded ordering on the async fork (CONTEXT-3 Decision 1
    — sequential `await` in batch override).
  - Trace decorator firing on the caller-tx path (Phase 1 spike memo §Design
    justification — `TraceDecorator -> RetryDecorator -> Handler`).
- No new issues added to `.shipyard/ISSUES.md` from this review (all findings are
  cosmetic Minor; none warrant tracking across sessions).
