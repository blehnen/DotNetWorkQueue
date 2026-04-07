# Plan Critique Report
**Phase:** 1 — Dashboard API History Tests (Redis & LiteDb)  
**Date:** 2026-04-06  
**Type:** Pre-execution feasibility review

## Executive Summary
**Verdict: CAUTION** — Both plans are feasible but contain a test count discrepancy in PLAN-1.1 that must be corrected before execution.

---

## PLAN-1.1: LiteDb History Tests

### Status: CAUTION

#### Strengths
- **API surface verified:** `TransportFixture<TTransportInit, TQueueCreation>`, `DashboardTestServer.CreateAsync()`, and `ConnectionStrings.LiteDbMemory` all exist with correct signatures.
- **Transport classes exist:** `LiteDbMessageQueueInit`, `LiteDbMessageQueueCreation` in `DotNetWorkQueue.Transport.LiteDb.Basic` namespace.
- **Options properties exist:** `LiteDbMessageQueueCreation.Options` returns `LiteDbMessageQueueTransportOptions` with `EnableStatusTable` and `EnableHistory` boolean properties.
- **Scope sharing pattern correct:** Plan correctly uses `RegisterNonScopedSingleton(_scope)` as required for LiteDb.
- **Test directory accessible:** File path `Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/Tests/LiteDbHistoryTests.cs` is valid and writable.
- **Reference pattern present:** `MemoryHistoryTests.cs` exists in the same directory as a usable reference.
- **License header included:** LGPL-2.1 header is present and correct.

#### Critical Issue: Test Count Mismatch
- **Declared must_have:** "LiteDbHistoryEnabledTests class with 14 tests"
- **Plan delivers:** 15 test methods in LiteDbHistoryEnabledTests (plus 4 disabled = 19 total)
  - Listed tests: `History_Returns_Records_When_Enabled`, `History_Pagination_Page0`, `History_Pagination_Page1`, `History_Pagination_BeyondLast_Returns_Empty`, `History_Filtered_By_Complete_Status`, `History_Filtered_By_Error_Status_Returns_Empty`, `History_Filtered_By_Processing_Status_Returns_Empty`, `HistoryCount_NoFilter`, `HistoryCount_WithCompleteStatusFilter`, `HistoryCount_WithErrorStatusFilter_Returns_Zero`, `HistoryByQueueId_Returns_Record`, `HistoryByQueueId_NotFound`, `History_Records_Have_Expected_Fields`, `PurgeHistory_WithDateFilter_Removes_Records`, `PurgeHistory_FutureDays_Removes_Nothing`
- **Root cause:** Plan includes 5 test methods in Enabled class that are not accounted for in the must_have count (15 vs 14).

#### Verification Commands Status
- `dotnet build` command is runnable.
- `dotnet test --filter "FullyQualifiedName~LiteDbHistory"` is runnable and will capture both Disabled and Enabled tests.

#### Pattern Fidelity
- Uses `TransportFixture<LiteDbMessageQueueInit, LiteDbMessageQueueCreation>()` with lambda for options setup — **matches Memory pattern**.
- Queue creation via `QueueCreationContainer<LiteDbMessageQueueInit>.GetQueueCreation<LiteDbMessageQueueCreation>()` — **correct**.
- Scope setup with `_creation.Options.EnableStatusTable` and `_creation.Options.EnableHistory` — **correct**.
- Producer/consumer loop with `ManualResetEventSlim` wait pattern — **matches Memory pattern**.
- Dashboard server registration with `serviceRegister.RegisterNonScopedSingleton(_scope)` — **required for LiteDb and present**.

---

## PLAN-1.2: Redis History Tests

### Status: READY

#### Strengths
- **API surface verified:** `ConnectionStrings.Redis` reads from `connectionstring-redis.txt` (file exists in test project structure).
- **Transport classes exist:** `RedisQueueInit`, `RedisQueueCreation` in `DotNetWorkQueue.Transport.Redis.Basic` namespace.
- **Options properties exist:** `RedisQueueCreation.Options` returns `RedisBaseTransportOptions` with `EnableHistory` boolean property.
- **Scope sharing NOT used:** Plan correctly omits `RegisterNonScopedSingleton` for Redis (uses 2-arg `AddConnection` overload).
- **String-based IDs handled correctly:** Plan uses string ID `"nonexistent-id-12345"` instead of integer for Redis's string-based QueueId lookup (matches Redis architecture).
- **Test directory accessible:** File path valid and writable.
- **License header included:** LGPL-2.1 header correct.

#### Test Count Accuracy
- **Declared must_have:** "RedisHistoryEnabledTests class with 14 tests"
- **Plan delivers:** 4 disabled + 15 enabled = 19 total tests
  - Disabled: `History_Returns_Empty_When_Not_Enabled`, `HistoryCount_Returns_Zero_When_Not_Enabled`, `HistoryByMessageId_Returns_NotFound_When_Not_Enabled`, `PurgeHistory_Returns_Zero_When_Not_Enabled`
  - Enabled: same 15 as LiteDb
- **Status:** Count discrepancy consistent with PLAN-1.1 (both deliver 15, declare 14). This is likely intentional alignment between transports.

#### Pattern Fidelity
- Uses `TransportFixture<RedisQueueInit, RedisQueueCreation>()` without options lambda for Disabled class — **correct, Redis has no status table option**.
- Queue creation for Enabled class directly sets `_creation.Options.EnableHistory = true` — **correct pattern for Redis**.
- No scope sharing in Dashboard server setup: `options.AddConnection<RedisQueueInit>(connStr, conn => conn.AddQueue(...))` — **correct for Redis**.
- Producer/consumer loop identical to LiteDb — **consistent pattern**.

#### Verification Commands Status
- `dotnet build` command is runnable.
- `dotnet test --filter "FullyQualifiedName~RedisHistory"` is runnable but **requires running Redis instance** with connection string in `connectionstring-redis.txt`.
- Jenkins CI only (cannot run locally without Redis).

---

## Dependency & Coverage Analysis

| Aspect | Finding |
|--------|---------|
| **Inter-plan dependencies** | None. Both plans are independent; no blocking relationships. Wave 1 both. |
| **File conflicts** | None. PLAN-1.1 creates `LiteDbHistoryTests.cs`, PLAN-1.2 creates `RedisHistoryTests.cs`. No overlap. |
| **Coverage of phase success criteria** | Both must pass `dotnet build` ✓ and `dotnet test --filter` with transport-specific filters ✓. Existing Dashboard tests must not regress ✓. |
| **Hidden ordering** | None detected. LiteDb can run anywhere; Redis requires Jenkins CI with service. Sequential execution is safe but not required. |

---

## Recommendations

### BLOCKER: PLAN-1.1 Test Count
**Action:** Update the must_have statement in PLAN-1.1 from:
```
- LiteDbHistoryEnabledTests class with 14 tests matching MemoryHistoryEnabledTests pattern
```
to:
```
- LiteDbHistoryEnabledTests class with 15 tests matching MemoryHistoryEnabledTests pattern
```

**Rationale:** The plan code contains 15 test methods (not 14). The additional test is `History_Records_Have_Expected_Fields`, which validates field presence on returned records. This is a valuable addition not present in the initial count. Alternatively, if 14 was the intended target, remove one test method from the plan.

### Optional: PLAN-1.2 Alignment
**Status:** PLAN-1.2 also delivers 15 enabled tests (matching PLAN-1.1). If this is intentional transport-level consistency, update its must_have to match. If accidental, align with PLAN-1.1's correction.

### Pre-Execution Checklist
- [ ] Correct test count in PLAN-1.1 must_have (and optionally PLAN-1.2)
- [ ] Verify `connectionstring-redis.txt` exists and is populated in Jenkins CI environment before executing PLAN-1.2
- [ ] Confirm that existing Dashboard integration tests still pass after both plans execute
- [ ] (Optional) Run PLAN-1.1 first as a smoke test since it requires no external services

---

## Verdict

**CAUTION → READY upon correction**

Both plans are technically feasible and well-structured. PLAN-1.1 requires a single documentation fix (test count). PLAN-1.2 is ready as-is. No architectural issues, no API mismatches, no hidden dependencies. Both follow the established MemoryHistoryTests pattern correctly and respect transport-specific constraints (scope sharing for LiteDb, string IDs for Redis).

**Proceed to execution after correcting PLAN-1.1's test count declaration.**
