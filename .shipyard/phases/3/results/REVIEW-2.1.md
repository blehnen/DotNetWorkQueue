# Review: Plan 2.1 (Phase 3 Wave 2 — Sync Fork)

## Verdict: PASS

Spec compliance clean. Fork body matches PLAN-2.1 lines 81–159 verbatim. Test-side comment-stripping deviation is sound for the current file and acceptable as a future regression sentinel. Findings are all Minor.

## Stage 1 — Correctness / Spec Compliance

| Spec item | Status | Evidence |
|---|---|---|
| Early-branch immediately after lazy-init block | PASS | `SendMessageCommandHandler.cs:103-109` — branch sits between the `_messageExpirationEnabled` lazy-init `}` (106) and the existing `var jobName = ...` (111). |
| Branch is `if (commandSend.ExternalTransaction != null) return HandleExternalTx(commandSend);` | PASS | Lines 108-109 exact match. |
| `HandleExternalTx(SendMessageCommand)` is `private long` | PASS | Line 202. |
| Casts `ExternalTransaction` to `SqlTransaction`, reuses `tx.Connection` as `SqlConnection` | PASS | Lines 207-208. |
| Job-exists query gated by non-empty `jobName` | PASS | Lines 219-225 — branch logic matches plan (negated combined predicate raises `DotNetWorkQueueException` on duplicate). |
| Body INSERT inline (uses `_commandCache.InsertMessageBody`) | PASS | Lines 228-248. |
| Metadata INSERT via `CreateMetaDataRecord` helper | PASS | Lines 262-263. |
| Status INSERT via `CreateStatusRecord` helper, gated on `_options.Value.EnableStatusTable` | PASS | Lines 265-268. |
| `SetJobLastKnownEventCommand` dispatched only for non-empty `jobName` | PASS | Lines 270-274. |
| No `tx.Commit()/Rollback()/Dispose()`, no `conn.Close()/Dispose()` in fork body | PASS | Greppable confirmed — only the documentation comment at line 276 references those tokens; no invocations. |
| `[SuppressMessage("Microsoft.Security", "CA2100:...")]` attribute on new method | PASS | Line 201. |
| Self-managed-tx path unchanged | PASS | Lines 111-184 byte-equivalent to pre-2.1 state aside from the 2-line early-branch insertion above it. |
| Smoke test 1: reflection signature | PASS | `SendMessageCommandHandlerForkSmokeTests.cs:37-50` asserts method exists with `(SendMessageCommand) -> long` private-instance signature. |
| Smoke test 2: source contains early-branch + dispatch | PASS | Lines 52-74 grep for the three signature strings. |
| Smoke test 3: lifecycle-ownership grep guard | PASS | Lines 76-122 with line-comment stripping to avoid false-positive on the load-bearing comment. |
| LGPL header on new test file | PASS | Lines 1-18 verbatim copy. |
| XML doc on new private method | PASS | Lines 187-200. |

## Stage 2 — Code Quality / Integration

### Critical
- None.

### Important
- None.

### Minor

1. **End-bound of fork-body extraction overreaches into sibling helpers** (`SendMessageCommandHandlerForkSmokeTests.cs:96`).
   `forkBody = content.Substring(forkStart, Math.Min(6000, content.Length - forkStart))` — `forkStart` indexes from `private long HandleExternalTx` (line 202). 6000 chars forward walks well past the method's closing `}` (line 279) and through `CreateStatusRecord` (line 289) + `CreateMetaDataRecord` (line 311) into the class's closing `}` at line 324. Today this is harmless because neither helper calls `.Commit()`/`.Rollback()`/`.Close()`/`.Dispose()`. But if someone later adds a lifecycle call to `CreateStatusRecord` for an unrelated reason, this test will fail under `HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose` with a misleading message — masking the actual call site.
   - **Remediation (optional):** scope the slice to the method body only — find the next `^        }$` (8-space indent + closing brace) after `forkStart` and slice to that index. Or look for the next `private ` / `public ` declaration as the end-bound. Either is ~5 lines of additional logic.

2. **Comment stripper does not handle string literals or block comments** (`SendMessageCommandHandlerForkSmokeTests.cs:109-112`).
   `line.IndexOf("//", StringComparison.Ordinal)` chops at the first `//` regardless of context. A future code line like `cmd.CommandText = "SELECT // not a comment"; tx.Commit();` would be truncated at the `//` inside the string and the `.Commit()` would slip past the test. Same applies to verbatim strings `@"http://..."` followed by code on the same line. Block comments `/* ... */` are not stripped at all (and could either false-positive on text inside or, less realistically, hide a real call after).
   - **Today:** no strings or block comments inside the fork body, so the stripper is correct for the current source. Acceptable risk.
   - **Remediation (optional, for future-proofing):** either (a) tighten the regex with a proper line-comment matcher that respects string-literal context (`Regex.Replace` with a token-pass), or (b) sidestep the problem entirely by renaming the source comment to use a different sentinel (e.g., the phrase "intentional non-invocation" instead of literal method names), matching the approach PLAN-2.2 took. The PLAN-2.2 source-side rephrasing is the lower-complexity solution; option (b) trades a small documentation tweak for eliminating the stripper entirely.

3. **Fragile relative source-file path** (`SendMessageCommandHandlerForkSmokeTests.cs:58-64, 82-87`).
   The `..\..\..\..\` walk-up assumes a 4-level-deep bin output structure (`bin/Debug/net10.0/` or similar). Already brittle to TFM changes, output-path overrides, or test-running tools that copy assemblies to staging directories. The plan acknowledged this and accepted "path resolution failure is itself a useful signal" — that justification is sound for now.
   - **Remediation (optional):** use `[CallerFilePath]` on a helper to anchor to the test's own source file location, then walk to the sibling project's source. This is fully robust across TFMs and bin layouts. Defer to a future refactor.

4. **Test names duplicate the source-file path string** (lines 58-64 vs 82-87).
   Identical path-resolution block copy-pasted in tests 2 and 3.
   - **Remediation (optional):** extract a `private static string GetHandlerSourcePath()` helper. ~8 lines saved; minor maintenance benefit.

### Positive

- Fork body matches the plan's code block byte-for-byte (verified line-by-line against PLAN-2.1.md lines 81-159).
- The branch placement (after lazy-init, before the self-managed path) preserves `_messageExpirationEnabled.Value` materialization — fork can read it on line 257.
- `CreateStatusRecord` + `CreateMetaDataRecord` reused unchanged — no SQL-builder duplication, honoring CONTEXT-3 §Decision 2.
- No `IDbConnectionFactory`/`IConnectionHolder` reach-through on the fork path — consistent with CONTEXT-3 hard rule.
- Trace decorator wraps both Handle paths because the fork is inside the same registered handler — observability preserved per CONTEXT-3 notes §Phase 1 spike.
- The fork's `id <= 0` guard mirrors the self-managed path's `if (id > 0) ... else throw` shape — same diagnostic message.
- Reflection assertion in Test 1 will break loudly on any rename or return-type change of `HandleExternalTx` — strong pin on the signature contract.
- Comment-stripping documented inline with rationale (lines 98-101).
- SUMMARY-2.1 §Decisions Made is candid about the test-side stripping deviation and notes PLAN-2.2's divergent source-side approach — review-friendly transparency.

## Deviation Audit

### Test-side line-comment stripping

- **Sound: YES** for the current source. The fork body (lines 202-279) contains no string literals with `//`, no block comments, and no verbatim strings — the stripper has no failure modes against the actual code under test.
- **Coverage — what the stripper handles correctly:**
  - Whole-line `//` comments (`trimmed.StartsWith("//")` skip).
  - Trailing inline `//` comments on code lines (substring cut).
  - The load-bearing documentation comment at line 276 (`// Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close().`) is correctly excised before the contains-checks.
- **Coverage — what the stripper does NOT handle:**
  - String literals containing `//` (e.g., `"http://..."` or `"// fake comment"`) — the substring-cut chops the line at the first `//` regardless of quote context. A future legitimate `.Commit()` placed AFTER a `//` inside a string on the same line would be silently dropped (false negative — invariant violation undetected).
  - Verbatim strings (`@"//..."`) — same failure as above.
  - Block comments (`/* ... */`) — completely ignored. A multi-line block comment containing `.Commit()` would false-positive; less realistic in practice.
  - Multi-line strings (interpolated `$"""..."""` raw strings) — not stripped; would survive into `forkCode` and could false-positive.
- **Will it catch a future legitimate `.Commit()` added inadvertently?** YES, in the common case (a developer adding `sqlTx.Commit();` as a standalone statement). NO, in the contrived case (the call hidden after a `//` inside a string literal on the same line — implausible but not impossible).
- **Comparison with PLAN-2.2's source-side rephrase:** PLAN-2.2 changes the SOURCE comment to avoid the substrings entirely, eliminating any need for stripping. That approach is simpler and more robust (no stripping logic to maintain, no edge cases). PLAN-2.1's test-side stripper preserves documentation value at the cost of test complexity. Both are valid; PLAN-2.2's is cheaper. Recommend the team align on PLAN-2.2's pattern for any future similar tests, but the current PLAN-2.1 implementation is shippable as-is.

### Trailing documentation comment retained in source

- The comment at line 276 (`// Deliberately NO trans.Commit() / Rollback() / Dispose() / sqlConn.Close(). The caller owns the transaction lifecycle.`) explicitly documents PROJECT.md §Success Criteria #7's invariant at the call site. Documentation value is real — a future maintainer reading the fork has an immediate explanation of why no commit happens.
- The cost is ~14 lines of test logic (the comment-stripping loop) plus the named edge cases above.
- **Verdict:** the cost/value trade is borderline. PLAN-2.2's source-rephrase preserves the same documentation intent in different words and avoids the test complexity, making it the slightly better pattern. But the current PLAN-2.1 implementation is correct, documented, and works — not worth churning a passing test for.

## Summary

**Verdict:** APPROVE
Critical: 0 | Important: 0 | Minor: 4

The sync handler fork is complete, faithful to the plan, and the structural smoke tests provide reasonable assurance pending Phase 6 integration coverage. The test-side comment-stripping approach is sound for today's source and is a known divergence from PLAN-2.2's source-rephrase approach — flagged for future consistency but not blocking.
