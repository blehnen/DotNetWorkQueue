# AES-256-GCM Message Interceptor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `AesMessageInterceptor` (AES-256-GCM authenticated encryption) as the recommended built-in encryption interceptor and deprecate `TripleDesMessageInterceptor`.

**Architecture:** New `IMessageInterceptor` implementation mirroring `GZipMessageInterceptor`/`TripleDesMessageInterceptor`. Envelope `[version(1)=0x01][nonce(12)][tag(16)][ciphertext]`, version byte authenticated as AES-GCM associated data, fresh CSPRNG nonce per message. 3DES retained + `[Obsolete]` for backward-compat (existing 3DES messages decrypt via the message graph). Dashboard gains an AES option so it can decrypt AES messages for display.

**Tech Stack:** C# / .NET 10 + .NET 8, `System.Security.Cryptography.AesGcm`, MSTest, NSubstitute, SimpleInjector.

Design spec: `docs/superpowers/specs/2026-07-06-aes-message-interceptor-design.md`.

## Global Constraints

- Targets `net10.0` and `net8.0`. `AesGcm` is available on both.
- LGPL-2.1 license header on every new source file (copy from any existing file in the same folder, e.g. `Interceptors/GZipMessageInterceptor.cs`).
- Release build enables `TreatWarningsAsErrors` — `[Obsolete]` references in **non-test** code (core lib, dashboard) become CS0618 errors and MUST be `#pragma`-suppressed with justification.
- AES-256 only: key is exactly 32 bytes. Nonce 12 bytes, tag 16 bytes.
- Nonce source MUST be `System.Security.Cryptography.RandomNumberGenerator` (CSPRNG), never `System.Random`.
- Follow existing interceptor conventions: `Guard.NotNull` for arg checks, `DisplayName`, `BaseType => GetType()`, config class in the same file as the interceptor.
- Work on branch `aes-message-interceptor` (already created; spec already committed there). Regular (non-draft) PR at the end.

## File Structure

- **Create** `Source/DotNetWorkQueue/Interceptors/AesMessageInterceptor.cs` — `AesMessageInterceptor` + `AesMessageInterceptorConfiguration`.
- **Create** `Source/DotNetWorkQueue.Tests/Interceptors/AesMessageInterceptorTests.cs` — unit tests.
- **Modify** `Source/DotNetWorkQueue/Interceptors/TripleDesMessageInterceptor.cs` — `[Obsolete]` on both types.
- **Modify** `Source/DotNetWorkQueue/IMessageInterceptor.cs` — add `<seealso cref="AesMessageInterceptor"/>`.
- **Modify** `Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs` — `#pragma` around the 3DES usage; add AES/3DES coexistence test.
- **Modify** `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardInterceptorOptions.cs` — add `Aes` property + `AesInterceptorOptions`.
- **Modify** `Source/DotNetWorkQueue.Dashboard.Api/Configuration/InterceptorConfigurationBuilder.cs` — AES branch; `#pragma` around 3DES branch.
- **Modify** `Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/InterceptorConfigurationBuilderTests.cs` and `DashboardInterceptorOptionsTests.cs` — AES coverage.
- **Create** an AES round-trip integration test under `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/`.
- **Modify** docs: `docker/dashboard/appsettings.example.json`, `docker/dashboard/README.md`, `docker/dashboard/DOCKERHUB.md`, `Changelog.md`, `Source/Directory.Build.props` (version bump).
- **Modify** wiki — separate local repo at `/mnt/f/git/DotNetWorkQueue.wiki` (its own git repo/commit): the encryption/interceptor page — document AES, key generation, migration guide (producer AES / consumer AES+3DES), 3DES deprecation + removal timeline.

---

### Task 1: AesMessageInterceptor + configuration (core crypto)

**Files:**
- Create: `Source/DotNetWorkQueue/Interceptors/AesMessageInterceptor.cs`
- Test: `Source/DotNetWorkQueue.Tests/Interceptors/AesMessageInterceptorTests.cs`

**Interfaces:**
- Consumes: `IMessageInterceptor`, `MessageInterceptorResult` (from `Source/DotNetWorkQueue/IMessageInterceptor.cs`); `DotNetWorkQueueException` (`DotNetWorkQueue.Exceptions`); `Guard` (`DotNetWorkQueue.Validation`).
- Produces:
  - `AesMessageInterceptorConfiguration(byte[] key)` — `Key` (32 bytes; throws `ArgumentException` otherwise).
  - `AesMessageInterceptor(AesMessageInterceptorConfiguration configuration)` — `MessageToBytes(byte[], IReadOnlyDictionary<string,object>) : MessageInterceptorResult`, `BytesToMessage(byte[], IReadOnlyDictionary<string,object>) : byte[]`, `DisplayName`, `BaseType`.
  - Envelope: `[0x01][nonce 12][tag 16][ciphertext]`.

- [ ] **Step 1: Write the failing tests**

Create `Source/DotNetWorkQueue.Tests/Interceptors/AesMessageInterceptorTests.cs` (LGPL header + this body):

```csharp
using System;
using System.Security.Cryptography;
using System.Text;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    [TestClass]
    public class AesMessageInterceptorTests
    {
        private static byte[] NewKey() { var k = new byte[32]; RandomNumberGenerator.Fill(k); return k; }
        private static AesMessageInterceptor New(byte[] key) => new(new AesMessageInterceptorConfiguration(key));

        [TestMethod]
        public void Config_RejectsNon32ByteKey()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new AesMessageInterceptorConfiguration(new byte[16]));
        }

        [TestMethod]
        public void RoundTrip_ReturnsOriginal()
        {
            var key = NewKey();
            var interceptor = New(key);
            foreach (var size in new[] { 0, 1, 150, 100_000 })
            {
                var original = new byte[size];
                RandomNumberGenerator.Fill(original);
                var encrypted = interceptor.MessageToBytes(original, null);
                Assert.IsTrue(encrypted.AddToGraph);
                var decrypted = New(key).BytesToMessage(encrypted.Output, null);
                CollectionAssert.AreEqual(original, decrypted);
            }
        }

        [TestMethod]
        public void Envelope_HasVersionNonceTagHeader()
        {
            var enc = New(NewKey()).MessageToBytes(Encoding.UTF8.GetBytes("hello"), null);
            Assert.AreEqual((byte)0x01, enc.Output[0]);
            Assert.AreEqual(1 + 12 + 16 + "hello"u8.Length, enc.Output.Length);
        }

        [TestMethod]
        public void SamePlaintext_ProducesDifferentCiphertext()
        {
            var interceptor = New(NewKey());
            var body = Encoding.UTF8.GetBytes("same message");
            var a = interceptor.MessageToBytes(body, null).Output;
            var b = interceptor.MessageToBytes(body, null).Output;
            CollectionAssert.AreNotEqual(a, b); // random nonce
        }

        [TestMethod]
        public void TamperedCiphertext_Throws()
        {
            var key = NewKey();
            var enc = New(key).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            enc[^1] ^= 0xFF; // flip last ciphertext byte
            // AesGcm throws AuthenticationTagMismatchException : CryptographicException -> use Throws<> (T-or-derived)
            Assert.Throws<CryptographicException>(() => New(key).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void TamperedVersionByte_Throws()
        {
            var key = NewKey();
            var enc = New(key).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            enc[0] = 0x02; // unknown version -> rejected before decrypt
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => New(key).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void WrongKey_Throws()
        {
            var enc = New(NewKey()).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            Assert.Throws<CryptographicException>(() => New(NewKey()).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void ShortInput_Throws()
        {
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => New(NewKey()).BytesToMessage(new byte[5], null));
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~AesMessageInterceptorTests"`
Expected: FAIL — `AesMessageInterceptor` / `AesMessageInterceptorConfiguration` do not exist (compile error).

- [ ] **Step 3: Write the implementation**

Create `Source/DotNetWorkQueue/Interceptors/AesMessageInterceptor.cs` (LGPL header + this body):

```csharp
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// Encrypts/decrypts a byte array using AES-256-GCM (authenticated encryption).
    /// Envelope: [version(1)=0x01][nonce(12)][tag(16)][ciphertext]. The version byte is
    /// authenticated as associated data, so it cannot be altered without failing the tag.
    /// </summary>
    /// <seealso cref="GZipMessageInterceptor"/>
    public class AesMessageInterceptor : IMessageInterceptor
    {
        private const byte Version = 0x01;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int HeaderSize = 1 + NonceSizeBytes + TagSizeBytes;

        private readonly AesMessageInterceptorConfiguration _configuration;

        /// <summary>Initializes a new instance of the <see cref="AesMessageInterceptor"/> class.</summary>
        public AesMessageInterceptor(AesMessageInterceptorConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            _configuration = configuration;
            DisplayName = "AES";
        }

        /// <inheritdoc />
        public MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);

            var nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(nonce); // CSPRNG, not System.Random

            var tag = new byte[TagSizeBytes];
            var ciphertext = new byte[input.Length];
            var associatedData = new[] { Version };

            using (var aes = new AesGcm(_configuration.Key, TagSizeBytes))
            {
                aes.Encrypt(nonce, input, ciphertext, tag, associatedData);
            }

            var output = new byte[HeaderSize + ciphertext.Length];
            output[0] = Version;
            Buffer.BlockCopy(nonce, 0, output, 1, NonceSizeBytes);
            Buffer.BlockCopy(tag, 0, output, 1 + NonceSizeBytes, TagSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, output, HeaderSize, ciphertext.Length);

            return new MessageInterceptorResult(output, true, GetType());
        }

        /// <inheritdoc />
        public byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);
            if (input.Length < HeaderSize)
                throw new DotNetWorkQueueException("AES envelope is too short to contain the version, nonce, and tag.");
            if (input[0] != Version)
                throw new DotNetWorkQueueException($"Unsupported AES envelope version 0x{input[0]:X2}; expected 0x{Version:X2}.");

            var nonce = new byte[NonceSizeBytes];
            var tag = new byte[TagSizeBytes];
            var ciphertext = new byte[input.Length - HeaderSize];
            Buffer.BlockCopy(input, 1, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(input, 1 + NonceSizeBytes, tag, 0, TagSizeBytes);
            Buffer.BlockCopy(input, HeaderSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];
            var associatedData = new[] { Version };
            using (var aes = new AesGcm(_configuration.Key, TagSizeBytes))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            }
            return plaintext;
        }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public Type BaseType => GetType();
    }

    /// <summary>
    /// Configuration for <see cref="AesMessageInterceptor"/>. AES-256 requires a 32-byte key.
    /// The nonce is generated per message and is not part of this configuration.
    /// </summary>
    public class AesMessageInterceptorConfiguration
    {
        private const int KeySizeBytes = 32;

        /// <summary>Initializes a new instance of the <see cref="AesMessageInterceptorConfiguration"/> class.</summary>
        /// <param name="key">The AES-256 key; must be exactly 32 bytes.</param>
        public AesMessageInterceptorConfiguration(byte[] key)
        {
            Guard.NotNull(() => key, key);
            if (key.Length != KeySizeBytes)
                throw new ArgumentException($"AES-256 requires a {KeySizeBytes}-byte key; received {key.Length}.", nameof(key));
            Key = key;
        }

        /// <summary>Gets the AES-256 key (32 bytes).</summary>
        public byte[] Key { get; }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~AesMessageInterceptorTests"`
Expected: PASS (8 tests).

- [ ] **Step 5: Verify Release build (warnings-as-errors) is clean**

Run: `dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Release`
Expected: `0 Warning(s), 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add Source/DotNetWorkQueue/Interceptors/AesMessageInterceptor.cs Source/DotNetWorkQueue.Tests/Interceptors/AesMessageInterceptorTests.cs
git commit -m "feat: add AesMessageInterceptor (AES-256-GCM)"
```

---

### Task 2: Deprecate TripleDesMessageInterceptor

**Files:**
- Modify: `Source/DotNetWorkQueue/Interceptors/TripleDesMessageInterceptor.cs`
- Modify: `Source/DotNetWorkQueue/IMessageInterceptor.cs`
- Modify: `Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs`

**Interfaces:**
- Produces: `TripleDesMessageInterceptor` and `TripleDesMessageInterceptorConfiguration` become `[Obsolete]` (warning). No signature changes.

- [ ] **Step 1: Add `[Obsolete]` to both 3DES types**

In `TripleDesMessageInterceptor.cs`, add `using System.Diagnostics.CodeAnalysis;` is not needed; add the attribute directly above `public class TripleDesMessageInterceptor` and above `public class TripleDesMessageInterceptorConfiguration`:

```csharp
[Obsolete("TripleDES is deprecated (NIST SP 800-131A) and vulnerable to Sweet32. Use AesMessageInterceptor (AES-256-GCM). This type is retained so existing 3DES-encrypted messages remain decryptable and will be removed in a future major version. See the encryption migration guide.")]
```

- [ ] **Step 2: Add AES seealso to the interface docs**

In `Source/DotNetWorkQueue/IMessageInterceptor.cs`, in the `IMessageInterceptor` XML doc block, add below the existing `<seealso cref="TripleDesMessageInterceptor"/>`:

```csharp
    /// <seealso cref="AesMessageInterceptor"/>
```
(cref to an obsolete type does not produce CS0618, so no suppression needed here.)

- [ ] **Step 3: Suppress CS0618 at the existing 3DES unit-test usage**

In `Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs`, wrap the `new TripleDesMessageInterceptor(...)` construction (inside `Interceptor_Multiple_Interceptors`) so the test still exercises the deprecated path:

```csharp
#pragma warning disable CS0618 // deliberately testing the deprecated 3DES interceptor
                new TripleDesMessageInterceptor(
                    new TripleDesMessageInterceptorConfiguration(
                        Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                        Convert.FromBase64String("aaaaaaaaaaa=")))
#pragma warning restore CS0618
```

- [ ] **Step 4: Verify Release build of core is clean**

Run: `dotnet build "Source/DotNetWorkQueue/DotNetWorkQueue.csproj" -c Release`
Expected: `0 Warning(s), 0 Error(s)` (the `[Obsolete]` type is defined here but no non-suppressed reference remains in the core project).

- [ ] **Step 5: Run the interceptor unit tests**

Run: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~Interceptors"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add Source/DotNetWorkQueue/Interceptors/TripleDesMessageInterceptor.cs Source/DotNetWorkQueue/IMessageInterceptor.cs Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs
git commit -m "feat: deprecate TripleDesMessageInterceptor in favor of AES"
```

---

### Task 3: Coexistence / backward-compat test

Proves the migration model: a 3DES-encrypted message decrypts when 3DES is in the consumer's pool, and AES round-trips — the graph selects the right decryptor.

**Files:**
- Modify: `Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs`

**Interfaces:**
- Consumes: `MessageInterceptors`, `InterceptorFactory`, `IContainerFactory` (as used elsewhere in this test file).

- [ ] **Step 1: Write the failing coexistence test**

Add to `InterceptionTest.cs` (uses the same `Substitute.For<IContainerFactory>()` pattern already in the file):

```csharp
[TestMethod]
public void Coexistence_ConsumerPoolDecryptsBoth()
{
    var aesKey = new byte[32];
    System.Security.Cryptography.RandomNumberGenerator.Fill(aesKey);
    var aesCfg = new AesMessageInterceptorConfiguration(aesKey);

#pragma warning disable CS0618 // migration scenario: legacy 3DES stays in the consumer pool
    var tdesCfg = new TripleDesMessageInterceptorConfiguration(
        Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
        Convert.FromBase64String("aaaaaaaaaaa="));

    // Producer registers AES only
    var producer = new MessageInterceptors(
        new List<IMessageInterceptor> { new AesMessageInterceptor(aesCfg) },
        new InterceptorFactory(Substitute.For<IContainerFactory>()));

    // A legacy producer that used 3DES only
    var legacyProducer = new MessageInterceptors(
        new List<IMessageInterceptor> { new TripleDesMessageInterceptor(tdesCfg) },
        new InterceptorFactory(Substitute.For<IContainerFactory>()));

    // Consumer registers BOTH — the pool of decryptors
    var consumer = new MessageInterceptors(
        new List<IMessageInterceptor> { new AesMessageInterceptor(aesCfg), new TripleDesMessageInterceptor(tdesCfg) },
        new InterceptorFactory(Substitute.For<IContainerFactory>()));
#pragma warning restore CS0618

    var body = Encoding.UTF8.GetBytes("coexistence body");

    var aesMsg = producer.MessageToBytes(body, null);
    CollectionAssert.AreEqual(body, consumer.BytesToMessage(aesMsg.Output, aesMsg.Graph, null));

    var tdesMsg = legacyProducer.MessageToBytes(body, null);
    CollectionAssert.AreEqual(body, consumer.BytesToMessage(tdesMsg.Output, tdesMsg.Graph, null));
}
```

- [ ] **Step 2: Run it**

Run: `dotnet test "Source/DotNetWorkQueue.Tests/DotNetWorkQueue.Tests.csproj" --filter "FullyQualifiedName~Coexistence_ConsumerPoolDecryptsBoth"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Tests/Interceptors/InterceptionTest.cs
git commit -m "test: AES/3DES consumer-pool coexistence"
```

---

### Task 4: Dashboard AES options

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardInterceptorOptions.cs`
- Modify: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/DashboardInterceptorOptionsTests.cs`

**Interfaces:**
- Produces: `DashboardInterceptorOptions.Aes : AesInterceptorOptions`; `AesInterceptorOptions { bool Enabled = true; string Key; }` (Key is Base64, no IV).

- [ ] **Step 1: Add the AES option to the options model**

In `DashboardInterceptorOptions.cs`, add a property to `DashboardInterceptorOptions`:

```csharp
        /// <summary>
        /// Gets or sets AES-256-GCM encryption interceptor options.
        /// Set to null (or omit from JSON) to disable.
        /// </summary>
        public AesInterceptorOptions Aes { get; set; }
```

and a new class in the same file:

```csharp
    /// <summary>
    /// JSON-bindable options for the AES-256-GCM message interceptor.
    /// </summary>
    public class AesInterceptorOptions
    {
        /// <summary>Gets or sets whether AES encryption is enabled. Default is true.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Gets or sets the AES-256 key as a Base64-encoded string (32 bytes when decoded).</summary>
        public string Key { get; set; }
    }
```

- [ ] **Step 2: Add/adjust a binding test**

Open `DashboardInterceptorOptionsTests.cs`, find the existing test that binds/round-trips `TripleDes` options, and add an analogous assertion for `Aes` (Enabled default true; Key round-trips). Mirror the existing test's style exactly. Run:

`dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~DashboardInterceptorOptionsTests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardInterceptorOptions.cs Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/DashboardInterceptorOptionsTests.cs
git commit -m "feat(dashboard): add AES interceptor options"
```

---

### Task 5: Dashboard builder AES branch + 3DES CS0618 suppression

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/Configuration/InterceptorConfigurationBuilder.cs`
- Modify: `Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/InterceptorConfigurationBuilderTests.cs`

**Interfaces:**
- Consumes: `AesMessageInterceptor`, `AesMessageInterceptorConfiguration` (Task 1); `AesInterceptorOptions` (Task 4).

- [ ] **Step 1: Write a failing builder test for AES**

In `InterceptorConfigurationBuilderTests.cs`, mirror the existing TripleDes test: build options with `Aes = new AesInterceptorOptions { Enabled = true, Key = Convert.ToBase64String(new byte[32]) }`, run the builder, and assert the returned registration action adds `typeof(AesMessageInterceptor)` and registers `AesMessageInterceptorConfiguration`. Also add a test that an AES key that is not 32 bytes when decoded throws `InvalidOperationException`. (Follow the exact assertion style already used for TripleDes in this file.)

Run: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~InterceptorConfigurationBuilderTests"`
Expected: FAIL (AES not handled yet).

- [ ] **Step 2: Add the AES branch and suppress CS0618 on the 3DES branch**

In `InterceptorConfigurationBuilder.cs`:
- Add near the other flags: `var enableAes = interceptorOptions.Aes is { Enabled: true };`
- Update the early return: `if (!enableGZip && !enableTripleDes && !enableAes) return null;`
- Add validation for AES (Key present + decodes to 32 bytes), mirroring the TripleDes validation block:

```csharp
            if (enableAes)
            {
                if (string.IsNullOrEmpty(interceptorOptions.Aes.Key))
                    throw new InvalidOperationException("Aes interceptor requires a Key (Base64-encoded).");
                if (Convert.FromBase64String(interceptorOptions.Aes.Key).Length != 32)
                    throw new InvalidOperationException("Aes interceptor Key must decode to 32 bytes (AES-256).");
            }
```
- Capture `var aesOptions = interceptorOptions.Aes;` alongside the other captures.
- Inside the returned `container => { ... }`, add:

```csharp
                if (enableAes)
                {
                    types.Add(typeof(AesMessageInterceptor));
                    container.Register(() =>
                        new AesMessageInterceptorConfiguration(Convert.FromBase64String(aesOptions.Key)),
                        LifeStyles.Singleton);
                }
```
- Wrap the existing `enableTripleDes` branch (both the validation block and the registration block) in:

```csharp
#pragma warning disable CS0618 // 3DES retained for decrypting legacy messages; deprecated
                ... existing TripleDes code ...
#pragma warning restore CS0618
```

- [ ] **Step 3: Run the builder tests**

Run: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Tests/DotNetWorkQueue.Dashboard.Api.Tests.csproj" --filter "FullyQualifiedName~InterceptorConfigurationBuilderTests"`
Expected: PASS.

- [ ] **Step 4: Verify Dashboard.Api Release build is clean**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Release`
Expected: `0 Warning(s), 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Api/Configuration/InterceptorConfigurationBuilder.cs Source/DotNetWorkQueue.Dashboard.Api.Tests/Configuration/InterceptorConfigurationBuilderTests.cs
git commit -m "feat(dashboard): register AES interceptor; suppress deprecated 3DES warning"
```

---

### Task 6: End-to-end integration test (Memory transport)

Proves AES works through a real send/receive and the graph re-creation. Encryption is transport-agnostic, so ONE transport (Memory, no external deps) is sufficient.

**Files:**
- Create: `Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Interceptors/AesInterceptorRoundTrip.cs`

**Interfaces:**
- Consumes: the Memory integration test harness. Follow the interceptor-registration pattern in `Source/DotNetWorkQueue.IntegrationTests.Shared/SharedSetup.cs` (search for `TripleDesMessageInterceptorConfiguration`) — it registers the interceptor Configuration in the container and adds the interceptor type to the `IMessageInterceptor` collection at queue creation.

- [ ] **Step 1: Write the test**

Create the test mirroring an existing Memory producer→consumer integration test (e.g. `Producer/SimpleProducer.cs` for structure), but configure the queue creation to register `AesMessageInterceptor` + `AesMessageInterceptorConfiguration(new byte[32] filled by RandomNumberGenerator)` using the same `SharedSetup` registration idiom used for 3DES. Send a POCO message on a Memory queue, consume it, and assert the received body equals what was produced (proving encrypt-on-send / decrypt-on-receive through the graph). Use a 32-byte key.

Concretely: copy the closest existing interceptor-enabled Memory integration test, swap the 3DES registration lines (`new TripleDesMessageInterceptorConfiguration(key, iv)` + `typeof(TripleDesMessageInterceptor)`) for the AES equivalents (`new AesMessageInterceptorConfiguration(key)` + `typeof(AesMessageInterceptor)`), and drop the IV.

- [ ] **Step 2: Run it**

Run: `dotnet test "Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/DotNetWorkQueue.Transport.Memory.Integration.Tests.csproj" --filter "FullyQualifiedName~AesInterceptorRoundTrip"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Transport.Memory.Integration.Tests/Interceptors/AesInterceptorRoundTrip.cs
git commit -m "test: AES interceptor end-to-end on the Memory transport"
```

---

### Task 7: Documentation, dashboard config example, changelog, version bump

**Files:**
- Modify: `docker/dashboard/appsettings.example.json`
- Modify: `docker/dashboard/README.md`, `docker/dashboard/DOCKERHUB.md`
- Modify: `Changelog.md`
- Modify: `Source/Directory.Build.props`

- [ ] **Step 1: Add an AES block to the dashboard appsettings example**

In `docker/dashboard/appsettings.example.json`, next to the existing interceptor config, add an `Aes` block with `Enabled` + a Base64 `Key` placeholder (a 32-byte example, clearly a placeholder), and add a comment/label marking the `TripleDes` block as legacy/deprecated. Keep it valid JSON.

- [ ] **Step 2: Update dashboard docs**

In `docker/dashboard/README.md` and `DOCKERHUB.md`, document the `Aes` interceptor option (Key only, no IV) as the recommended encryption and mark `TripleDes` as deprecated.

- [ ] **Step 3: Changelog entry (concise)**

In `Changelog.md`, add a concise entry: `Added AesMessageInterceptor (AES-256-GCM); deprecated TripleDesMessageInterceptor (removed in a future major).`

- [ ] **Step 4: Bump the version**

In `Source/Directory.Build.props`, bump `<Version>` (e.g. `0.9.42` → `0.9.43`).

- [ ] **Step 4b: Update the wiki (separate repo)**

In `/mnt/f/git/DotNetWorkQueue.wiki` (its own git repo), update the encryption/interceptor page: document `AesMessageInterceptor` (AES-256-GCM, 32-byte key generation, no IV), the migration guide (producer registers AES; consumer registers AES + 3DES; drain then drop 3DES), and mark 3DES deprecated with the removal timeline (link the removal issue from Task 8). Commit in that repo (`git -C /mnt/f/git/DotNetWorkQueue.wiki commit`). Do not push until the feature is released.

- [ ] **Step 5: Verify the no-tests solution builds in Release**

Run: `dotnet build "Source/DotNetWorkQueueNoTests.sln" -c Release -p:CI=true`
Expected: `0 Error(s)` (warnings only if pre-existing/unrelated).

- [ ] **Step 6: Commit**

```bash
git add docker/dashboard/appsettings.example.json docker/dashboard/README.md docker/dashboard/DOCKERHUB.md Changelog.md Source/Directory.Build.props
git commit -m "docs: document AES interceptor; changelog + version bump"
```

---

### Task 8: Open PR + create the samples-tracking issue

- [ ] **Step 1: Push and open a regular (non-draft) PR**

```bash
git push -u origin aes-message-interceptor
gh pr create --title "Add AES-256-GCM message interceptor; deprecate TripleDES" --body "<summary from the spec: what/why, migration (producer AES / consumer AES+3DES), deprecation + planned removal, test coverage>"
```

- [ ] **Step 2: Create the samples follow-up issue (now that the feature is in flight)**

Create a GitHub issue in `blehnen/DotNetWorkQueue.Samples` titled "Update samples to use AesMessageInterceptor (after release)", noting it depends on the published NuGet package and should replace 3DES usages with AES. Link it from the PR body.

- [ ] **Step 3: Create the 3DES removal tracking issue**

Create a GitHub issue in `blehnen/DotNetWorkQueue` titled "Remove deprecated TripleDesMessageInterceptor", targeting the **next major release (1.0.0), ~early 2027**. Body: 3DES was deprecated when AES-256-GCM landed (link this PR); removal is breaking (public API) so it ships in the next major; migration = switch producers to AES and drain 3DES-encrypted messages first. Cross-link the wiki migration guide. Reference this issue number from the wiki page (Task 7 Step 4b).

- [ ] **Step 4: Watch checks; address CodeRabbit/SonarCloud/Jenkins as they report.**

---

## Self-Review

**Spec coverage:**
- AES interceptor + config → Task 1. Envelope/AAD/CSPRNG/32-byte key → Task 1 code + tests.
- 3DES `[Obsolete]` + CS0618 sites (core/test) → Task 2; dashboard site → Task 5.
- Backward-compat/coexistence (producer/consumer + graph) → Task 3.
- Dashboard options + builder → Tasks 4-5.
- Integration test (Memory only) → Task 6.
- Wiki/dashboard docs/changelog/version bump → Task 7 (wiki itself is a separate repo, updated at release — noted, not a repo task here).
- Samples follow-up issue → Task 8 Step 2. Planned 3DES removal → tracked via the deprecation message + future major (out of scope here).

**Placeholder scan:** Tasks 4/5/6 reference "mirror the existing test" against **named existing files** with the exact 3DES→AES substitution spelled out — concrete references, not TBDs. Task 1 (the crypto risk area) has complete code. No unresolved placeholders.

**Type consistency:** `AesMessageInterceptor` / `AesMessageInterceptorConfiguration(byte[] key)` / `Key` / `MessageToBytes` / `BytesToMessage` / `AesInterceptorOptions { Enabled, Key }` are used consistently across Tasks 1, 3, 4, 5, 6.

**Note for executor:** the wiki lives outside this repo; do it at release. Confirm the exact interceptor-registration lines in `SharedSetup.cs` before writing Task 6 (the harness is the one area not fully shown here).
