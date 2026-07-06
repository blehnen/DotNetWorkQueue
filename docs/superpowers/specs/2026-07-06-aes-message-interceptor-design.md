# Design: AES-256-GCM Message Interceptor + TripleDES Deprecation

**Date:** 2026-07-06
**Status:** Approved (design); PR #181 merged to master — ready for implementation planning
**Driver:** SonarCloud S5547 (weak cipher, TripleDES) + modernize built-in encryption

## Summary

Add `AesMessageInterceptor` (AES-256-GCM authenticated encryption) as the recommended
built-in encryption interceptor, and deprecate the existing `TripleDesMessageInterceptor`.
3DES is retained and fully functional so existing 3DES-encrypted messages remain
decryptable, marked `[Obsolete]`, with a planned removal in a future major version.

This is a **releasable feature** (new public API) → changelog entry + version bump.

## Goals

- Provide a modern, authenticated (AEAD) encryption option out of the box.
- Fix the concrete weaknesses of the 3DES interceptor: 64-bit block (Sweet32), CBC with
  **no tamper detection**, and a **static IV** reused across every message.
- Preserve backward compatibility: 3DES-encrypted messages must keep decrypting.
- Give users a clear, simple migration path.

## Non-goals (YAGNI)

- **Not** a multi-algorithm crypto engine. The interceptor pattern is the extensibility
  seam — users needing ChaCha20, envelope encryption, HSM/KMS, etc. write their own
  interceptor. The base library ships one good encryption option + one compression option.
- **No key rotation** in v1 (the versioned envelope reserves room to add it later).
- **No** "3DES auto-detects AES and opts out" magic, and **no** first-class "decrypt-only"
  registration concept — the producer/consumer config split already provides the coexistence
  path (see Migration).

## Crypto design

- **Algorithm:** AES-256-GCM via `System.Security.Cryptography.AesGcm` (net8/net10).
- **Key:** 32 bytes (AES-256 only). `AesMessageInterceptorConfiguration(byte[] key)` validates
  `key.Length == 32`, else throws. Key lives only in the DI-registered config; never logged,
  never in the envelope.
- **Nonce:** fresh 12 bytes per message from
  `System.Security.Cryptography.RandomNumberGenerator.Fill(nonce)` — the CSPRNG, **not**
  `System.Random`.
- **Envelope:** `[version(1)=0x01][nonce(12)][tag(16)][ciphertext(n)]`.
- **Associated data:** the version byte is passed as AES-GCM AAD, so flipping it fails the
  auth tag (can't be used to trigger a different unpacker). Message headers are **not** AAD in
  v1 (keeps it simple; header-binding is a "write your own interceptor" concern).
- **Nonce-uniqueness limit (documented, not enforced):** random 12-byte nonces are safe to
  ~2^32 messages under a single key (NIST SP 800-38D). Far beyond typical workloads and
  strictly better than 3DES's fixed IV; extreme-volume users rotate keys (future format
  version).
- **AesGcm usage:** `using var aes = new AesGcm(key, tagSizeInBytes: 16);` then
  `Encrypt(nonce, input, ciphertext, tag, associatedData)` /
  `Decrypt(nonce, ciphertext, tag, plaintext, associatedData)`. `AesGcm` is `IDisposable`.

## Components

New files in `Source/DotNetWorkQueue/Interceptors/` (mirroring `GZipMessageInterceptor`):

- **`AesMessageInterceptor : IMessageInterceptor`**
  - `MessageToBytes`: generate nonce → encrypt → assemble envelope →
    `new MessageInterceptorResult(envelope, addToGraph: true, GetType())`. Encryption
    **always** applies (`addToGraph` always `true`, like 3DES; unlike GZip's conditional).
  - `BytesToMessage`: read+validate version byte (unknown → throw) → slice nonce/tag/ct
    (guard min length) → decrypt → return plaintext.
  - `DisplayName = "AES"`, `BaseType => GetType()`.
- **`AesMessageInterceptorConfiguration`** — ctor `(byte[] key)`, 32-byte validation.

Changed:

- **`TripleDesMessageInterceptor` + `TripleDesMessageInterceptorConfiguration`** →
  `[Obsolete(...)]` (warning), unchanged behavior.
- **`IMessageInterceptor`** XML docs → add `<seealso cref="AesMessageInterceptor"/>` (cref to
  an obsolete type does not warn).
- **Dashboard** (`DotNetWorkQueue.Dashboard.Api/Configuration/`):
  - `DashboardInterceptorOptions` → add `Aes` property.
  - New `AesInterceptorOptions { bool Enabled; string Key; }` — **Key only, no IV** (nicer
    than 3DES).
  - `InterceptorConfigurationBuilder` → add `enableAes` branch (validate Base64 key → 32
    bytes; register `AesMessageInterceptor` + config). Update the early-return guard.

No new framework plumbing: the existing interceptor graph + `InterceptorFactory` re-creation
handle send/receive.

## Backward compatibility & migration

**Mechanism (verified in code):** on **send**, `MessageInterceptors.MessageToBytes` runs every
interceptor in the registered collection. On **receive**, `BytesToMessage` reads the message's
graph and resolves each recorded type via `GetInterceptor(type)` — which checks the registered
collection first, then **falls back to `InterceptorFactory.Create(type)`** (resolve-by-type
from DI). So an interceptor can decrypt by type even if it is not in the send chain.

**Tier 1 — drain & swap (documented primary path):** stop producing 3DES, let consumers drain
the queue, then replace `TripleDesMessageInterceptor` with `AesMessageInterceptor` in the
registration. Zero framework change. Covers the large majority of real migrations, since queue
messages are normally consumed within seconds/minutes.

**Tier 2 — coexist (long-lived/durable messages):** interceptors are configured **per queue
instance**, and producers and consumers are separate queues with separate configs. The
per-message **graph** drives which interceptors run on the receive side, so:
- **Producer:** register `[GZip, AES]` (was `[GZip, 3DES]`) — new messages encrypt with AES.
- **Consumer:** register `[GZip, AES, 3DES]` — the collection is a *pool of decryptors*; each
  message's graph selects which actually run (new → AES, old → 3DES). The consumer never calls
  `MessageToBytes` on inbound messages, so **3DES on a consumer only ever decrypts** — it cannot
  re-encrypt. Drop 3DES from the consumer once the 3DES backlog is drained.
- Edge case for docs: a single queue that both consumes *and* re-produces to itself with the
  same config should keep the producer/consumer configs separate.

No "decrypt-only" registration concept is needed — the producer/consumer split + graph-driven
receive already provide it.

**Dashboard:** display-only and likewise **graph-driven** — it registers both AES and 3DES, and
each message's graph selects the right decryptor (new → AES, old → 3DES). It never sends, so
there is no double-encrypt concern; it needs both to render new and legacy message bodies.

## Deprecation mechanics

- `[Obsolete("TripleDES is deprecated (NIST SP 800-131A) and vulnerable to Sweet32; use
  AesMessageInterceptor (AES-256-GCM). Retained so existing 3DES-encrypted messages remain
  decryptable. Will be removed in a future major version — see the migration guide.")]` on both
  3DES types. Severity = warning (not error).
- **CS0618 reference sites** (kept for back-compat) get scoped `#pragma warning disable/restore
  CS0618` + justification, so the library's own Release (`TreatWarningsAsErrors`) build stays
  green:
  - `Dashboard.Api/Configuration/InterceptorConfigurationBuilder.cs` (keeps 3DES for
    decrypt-display).
  - Integration-test shared setup (`IntegrationTests.Shared/...`) — kept to prove the deprecated
    decrypt path still works.
  - `DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs` — deprecated-path unit test.
- **Planned removal:** future major version + a tracked GitHub issue, linked from the migration
  guide.

## Error handling

- Decrypt failure (wrong key / tampered ciphertext, tag, nonce, or version) → `AesGcm` throws
  `CryptographicException`, which propagates into the framework's existing poison-message
  handling (same path 3DES `TransformFinalBlock` failures take today).
- Malformed/too-short envelope → clear exception (guarded before slicing).
- Null input → guarded (as existing interceptors do).

## Testing

- **Unit** (`DotNetWorkQueue.Tests/Interceptors/`, primary crypto coverage):
  round-trip (empty/small/large), nonce-uniqueness, envelope shape, tamper detection
  (ciphertext/tag/nonce/version byte each → throws), wrong key → throws, malformed input →
  throws, key ≠ 32 bytes → config throws, `AddToGraph` always true.
- **Backward-compat proof:** a 3DES-encrypted message decrypts via the receive-path factory
  fallback; an AES message round-trips through the registrar (coexistence works).
- **Integration — deliberately narrow:** encryption is transport-agnostic (byte transform
  before the transport), so AES gets **one** end-to-end path through the **Memory** transport
  (local), not fanned across all transports. Existing 3DES integration coverage stays as-is.
- **Dashboard** (`Dashboard.Api.Tests`): AES coverage for `InterceptorConfigurationBuilder`,
  mirroring the 3DES tests.

## Docs, samples, release

- **Wiki** (not NuGet-blocked → ships with the feature/release): encryption/interceptor page —
  document `AesMessageInterceptor` + key-generation guidance, mark 3DES deprecated, add the
  migration guide (Tier-1 primary, Tier-2 note).
- **Dashboard config docs:** `docker/dashboard/appsettings.example.json` gets an `Aes` block
  (`TripleDes` relabeled legacy); `docker/dashboard/README.md` + `DOCKERHUB.md` updated.
- **Changelog + version bump** (releasable feature).
- **Samples — separate follow-up, out of this feature's scope:** the
  [DotNetWorkQueue.Samples](https://github.com/blehnen/DotNetWorkQueue.Samples) repo consumes
  DotNetWorkQueue via **NuGet**, so it cannot reference `AesMessageInterceptor` until the new
  version is **published**. Track via a **GitHub issue created once this feature is in flight**
  (PR open); do the sample updates after release.

## Sequencing

1. **Gate:** no code changes until PR #181 is merged to master (master must be current first).
2. Implement code + tests + wiki + dashboard docs on a feature branch.
3. When the feature PR is opened, create the samples-tracking GitHub issue.
4. Merge → release (changelog + version bump) → update samples against the published package.
5. Future major: remove 3DES (tracked issue).

## Open items to confirm during implementation

- Exact wiki page(s) for encryption/interceptors.
- Exact sample files that reference 3DES (for the follow-up issue).
