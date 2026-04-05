# Security Audit Report — Phase 3

## Executive Summary

**Verdict:** PASS (with conditions)
**Risk Level:** Medium

The Docker image and self-contained dashboard mode are structurally sound. No secrets are baked into the image, the middleware pipeline is ordered correctly, and there are no injection vulnerabilities in the authentication path. The most important issue is that authentication is **opt-in and off by default**: the example config ships with a placeholder `PasswordHash` value that will fail to match any real password, and if a user deploys the container without setting credentials the dashboard is fully open to anyone who can reach port 8080. The API key protecting the backend API controllers is similarly empty by default. A second concern is that Swagger UI is enabled by default (`"EnableSwagger": true`) and is reachable without authentication, giving any network-adjacent user a browsable, interactive interface to every queue operation. These are not exploitable vulnerabilities in the shipped code, but they represent a high-likelihood misconfiguration path for real deployments.

### What to Do

| Priority | Finding | Location | Effort | Action |
|----------|---------|----------|--------|--------|
| 1 | Auth disabled by default — no warning at startup | `Program.cs:57` | Trivial | Print the same style of warning that already exists for the username-without-hash case; log a WARN when `authEnabled == false` at startup |
| 2 | Swagger UI open with no auth check | `DashboardExtensions.cs:197` | Small | Gate `UseSwagger`/`UseSwaggerUI` behind the same cookie auth middleware; or disable by default |
| 3 | Container runs as root (implicit) | `Dockerfile:53` | Trivial | Add `USER app` before `ENTRYPOINT` |
| 4 | .NET 8 SDK fetched over curl with no checksum | `Dockerfile:6-10` | Small | Verify the tarball SHA-512 after download |
| 5 | Plain HTTP only — no TLS guidance | `README.md` / `Dockerfile:56` | Small | Document TLS termination requirement in README; consider HTTPS redirect or at least a warning |
| 6 | `AllowedHosts: "*"` in example config | `appsettings.example.json:7` | Trivial | Change to a restrictive placeholder; document what to set |
| 7 | `TrustServerCertificate=true` in SQL Server example | `appsettings.example.json:23` | Trivial | Add comment warning that this disables TLS cert validation |
| 8 | API key empty string = no auth on API controllers | `appsettings.example.json:19` | Trivial | Add comment making the consequence explicit |

### Themes

- **Security is opt-in, not opt-out.** Both authentication layers (UI login, API key) default to disabled. Insecure deployments require zero misconfiguration — the defaults produce them.
- **Swagger leaks the attack surface.** An unauthenticated Swagger UI endpoint is inconsistent with the intent to require a login before accessing dashboard data.
- **Container hardening is incomplete.** The image is otherwise well-constructed (multi-stage, no secrets in layers, minimal packages) but runs as root and fetches a secondary SDK binary without integrity verification.

---

## Detailed Findings

### Important

**[I1] Dashboard UI authentication is disabled by default; no startup warning**
- **Location:** `Source/DotNetWorkQueue.Dashboard.Ui/Program.cs:57`
- **Description:** `authEnabled` is `false` when either `Username` or `PasswordHash` is empty. The example config ships with `PasswordHash: "REPLACE_WITH_SHA256_HEX_OF_YOUR_PASSWORD"` — a non-empty placeholder string. A user who copies the example file and changes only the connection strings (a plausible path) will have `authEnabled = true` because both fields are non-empty, but will be unable to log in because the placeholder hash matches no real password. The dashboard will show the login page but every attempt will fail, giving a false sense of security: the container is running, login appears enforced, but no user can ever authenticate. Separately, if a user omits both fields entirely, the dashboard is wide open with no warning at the console (the existing warning fires only when username is set but hash is absent, `Program.cs:74`).
- **Impact:** Depending on misconfiguration path: either permanent lockout (placeholder hash), or fully unauthenticated dashboard access to queue data, error messages, and message body editing. (OWASP A07:2021 — Identification and Authentication Failures; CWE-306)
- **Remediation:** (1) Change the placeholder `PasswordHash` in `appsettings.example.json` to an empty string so the field is obviously unset. (2) Add a startup warning (matching the existing console warning style) when `authEnabled == false`, regardless of why. (3) Consider whether "auth disabled = allow all" should require an explicit opt-out flag rather than being the default.
- **Evidence:**
  ```csharp
  // Program.cs:57
  var authEnabled = authUsername.Length > 0 && authPasswordHash.Length > 0;
  // With example config: authUsername="admin", authPasswordHash="REPLACE_..." → authEnabled=true
  // but hash never matches any real password
  ```

**[I2] Swagger UI is reachable without authentication**
- **Location:** `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs:197-203`, `Program.cs:95-98`
- **Description:** `UseDotNetWorkQueueDashboard()` calls `app.UseSwagger()` and `app.UseSwaggerUI()` before `UseAuthentication()` is called in the middleware pipeline (`Program.cs:99`). The Swagger middleware runs first and serves `/swagger` and `/swagger/v1/swagger.json` to any unauthenticated caller. Additionally, the example config defaults `EnableSwagger: true`. An attacker who can reach the container gets a full interactive API explorer showing every endpoint, parameter, and schema.
- **Impact:** Information disclosure of the full API surface; enables targeted reconnaissance. If the API key is also empty (default), an attacker can execute any queue operation via Swagger without credentials. (OWASP A01:2021 — Broken Access Control; CWE-284)
- **Remediation:** Either (a) move Swagger middleware registration to after `UseAuthentication`/`UseAntiforgery` and add an `[Authorize]` requirement to the Swagger endpoint, or (b) default `EnableSwagger` to `false` and require explicit opt-in. Option (b) is simpler and appropriate for a production container image.
- **Evidence:**
  ```csharp
  // Program.cs — middleware order
  app.UseDotNetWorkQueueDashboard();  // line 95 — calls UseSwagger() here
  app.UseRouting();                    // line 98
  app.UseAuthentication();             // line 99 — too late for Swagger
  ```

**[I3] Container runs as root**
- **Location:** `docker/dashboard/Dockerfile:53` (WORKDIR /app), line 59 (ENTRYPOINT)
- **Description:** The runtime stage sets `WORKDIR /app` and runs the application but never sets a non-root user. `mcr.microsoft.com/dotnet/aspnet:10.0` includes a pre-created non-root user named `app` (UID 1654) expressly for this purpose. Without a `USER` directive, the .NET process runs as UID 0. If the process is compromised, the attacker has root inside the container.
- **Impact:** Container escape is easier as root; any writable host mounts are accessible as root; Linux capabilities are not dropped. (CIS Docker Benchmark 4.1; CWE-250 — Execution with Unnecessary Privileges)
- **Remediation:** Add two lines before `ENTRYPOINT`:
  ```dockerfile
  RUN chown -R app:app /app
  USER app
  ```
- **Evidence:** Dockerfile has no `USER` directive in the runtime stage.

**[I4] Secondary SDK tarball fetched without integrity verification**
- **Location:** `docker/dashboard/Dockerfile:6-10`
- **Description:** The .NET 8 SDK is downloaded via `curl` from `dotnetcli.azureedge.net` and extracted directly without verifying a checksum or signature. If the CDN is compromised, a build cache is poisoned, or the URL is intercepted, a malicious SDK could be installed silently. The version is pinned (`8.0.408`), which is good — but pinning a version does not prevent a substitution attack.
- **Impact:** Build-time supply chain compromise; malicious toolchain could inject code into the published application. (CWE-494 — Download of Code Without Integrity Check; SLSA Level 1 gap)
- **Remediation:** Download the official SHA-512 hash file alongside the tarball and verify before extracting:
  ```dockerfile
  RUN dotnet_version=8.0.408 \
      && curl -fSL -o dotnet8.tar.gz \
         "https://dotnetcli.azureedge.net/dotnet/Sdk/${dotnet_version}/dotnet-sdk-${dotnet_version}-linux-x64.tar.gz" \
      && curl -fSL -o dotnet8.tar.gz.sha512 \
         "https://dotnetcli.azureedge.net/dotnet/Sdk/${dotnet_version}/dotnet-sdk-${dotnet_version}-linux-x64.tar.gz.sha512" \
      && echo "$(cat dotnet8.tar.gz.sha512)  dotnet8.tar.gz" | sha512sum -c - \
      && tar -oxzf dotnet8.tar.gz -C /usr/share/dotnet ./sdk ./shared \
      && rm dotnet8.tar.gz dotnet8.tar.gz.sha512
  ```

---

### Advisory

- **Plain HTTP only, no TLS guidance** (`docker/dashboard/README.md`, `Dockerfile:56`) — The container listens on HTTP 8080 with no TLS and no documentation guidance to terminate TLS at a reverse proxy. Connection strings (including SQL Server credentials) traverse the network in plaintext in self-contained mode if TLS is not added externally. Add a README section explicitly requiring a TLS-terminating reverse proxy (nginx, Traefik, Caddy) in front of the container for any non-localhost deployment.

- **`AllowedHosts: "*"` in example config** (`appsettings.example.json:7`) — The wildcard disables ASP.NET Core's host header validation, making the application vulnerable to host header injection attacks (CWE-116). Change the placeholder to a comment-documented specific hostname or `localhost` and instruct users to set their actual hostname.

- **`TrustServerCertificate=true` in SQL Server example connection string** (`appsettings.example.json:23`) — This disables TLS certificate validation for the SQL Server connection, silently accepting any certificate including self-signed or attacker-supplied ones (CWE-295). Add a comment warning that this setting must be removed or replaced with proper certificate configuration for production.

- **API key defaults to empty string** (`appsettings.example.json:19`, `DashboardExtensions.cs:163`) — When `Dashboard:ApiKey` is empty, `ApiKeyAuthorizationFilter.OnAuthorization` returns immediately (line 44), applying no protection to any API controller endpoint. The example config and README do not clearly state that leaving this blank means the API is unprotected. Add an explicit comment in the example config and a startup log message when the API key is empty.

- **SHA-256 for password hashing is insufficient** (`Program.cs:116`) — SHA-256 without a salt or iteration count is fast by design, making it trivially brute-forceable against a stolen cookie or offline attack. An attacker who observes that the hash is SHA-256 hex can run commodity GPU cracking tools. This is a known and accepted design choice (per the project security model memory note), but it is worth documenting: production deployments should use a sufficiently random, long password to compensate for the absent key-stretching. Consider a future migration to BCrypt or PBKDF2. (CWE-916 — Use of Password Hash with Insufficient Computational Effort)

- **Swagger enabled by default exposes API schema even when API key is set** (`appsettings.example.json:18`) — Even with an API key, the full OpenAPI schema at `/swagger/v1/swagger.json` is served unauthenticated. Schema exposure is not directly exploitable but reduces security-by-obscurity for internal tooling. Default to `false`.

- **`libdl.so` symlink creation is architecture-specific** (`Dockerfile:48-51`) — The symlink targets `/usr/lib/x86_64-linux-gnu/`. On ARM64 (e.g., Apple Silicon Docker, AWS Graviton), the path is `/usr/lib/aarch64-linux-gnu/` and the symlink silently fails (the `2>/dev/null ||` fallback catches it). The fallback to `libc.so.6` may or may not work depending on glibc version. This is a reliability issue that could surface as a security misconfiguration (SQLite failing to load, silent degraded state) on non-x86 hosts.

---

## Cross-Component Analysis

**Authentication gap between UI and API in self-contained mode.** The UI enforces cookie authentication for Blazor pages. The API controllers are protected by `ApiKeyAuthorizationFilter`. In self-contained mode, the in-process HttpClient calls the API over localhost with the API key from config — this is coherent. However, `UseAuthentication()` is called in the pipeline but no `[Authorize]` attribute or `AuthorizationPolicy` is set on the API controllers unless `DashboardOptions.AuthorizationPolicy` is non-empty (it defaults to empty). This means a browser request that somehow bypasses the UI and hits `/api/v1/...` directly will pass through `UseAuthentication` (establishing no identity) and then hit `ApiKeyAuthorizationFilter` — if the API key is empty, the request succeeds with no credentials at all. The two auth layers (cookie for UI, API key for controllers) do not cross-validate: a valid cookie does not substitute for an API key, and vice versa.

**Swagger sits outside both auth layers.** As noted in [I2], Swagger middleware executes before `UseAuthentication`. Neither the cookie nor the API key protects it. The three-layer security model (UI auth → API key → controller action) has a gap at the documentation surface.

**Error handling is consistent.** `DashboardExceptionFilter` is registered globally and prevents unhandled exceptions from leaking stack traces in production. `Program.cs` uses `UseExceptionHandler` in non-development mode. The login failure path redirects to `/login?error=1` without disclosing whether the username or password was wrong — this is correct.

**Connection strings flow only through config, not code.** No connection string is constructed from user-supplied request input. The transport dispatch in `AddConnectionByTransport` uses a `switch` on a static string from config (not from HTTP requests), so there is no injection path from the API into connection string construction.

---

## Analysis Coverage

| Area | Checked | Notes |
|------|---------|-------|
| Code Security (OWASP) | Yes | Auth pipeline, error handling, login handler, API key filter reviewed |
| Secrets & Credentials | Yes | No secrets in Dockerfile layers, git-tracked files, or compiled output |
| Dependencies | Partial | No new NuGet packages added in Phase 3; transitive transport assemblies already present in prior phases; no new CVE surface identified |
| Infrastructure as Code | Yes | Dockerfile fully reviewed |
| Docker/Container | Yes | Multi-stage, no secrets in layers, root user finding documented |
| Configuration | Yes | Example config, AllowedHosts, TrustServerCertificate, API key defaults reviewed |

---

## Dependency Status

No new dependencies were introduced in Phase 3. The `ProjectReference` to `DotNetWorkQueue.Dashboard.Api` (and its transitive transport references) was already present or is pulling packages audited in prior phases. No new NuGet packages appear in the diff.

| Package | Version | Known CVEs | Status |
|---------|---------|-----------|--------|
| (no new packages in this phase) | — | — | — |

---

## IaC Findings

| Resource | Check | Status |
|----------|-------|--------|
| Dockerfile — multi-stage build | Secrets not baked into image layers | PASS |
| Dockerfile — base image pinned | `sdk:10.0` and `aspnet:10.0` use floating tags (not digest-pinned) | ADVISORY |
| Dockerfile — non-root user | No `USER` directive in runtime stage | FAIL |
| Dockerfile — minimal packages | Only `libsqlite3-0` added; `apt` cache cleared | PASS |
| Dockerfile — secondary download integrity | No checksum verification on .NET 8 SDK tarball | FAIL |
| Dockerfile — EXPOSE | Only port 8080 exposed | PASS |
| Dockerfile — health check | No `HEALTHCHECK` instruction | ADVISORY |
| appsettings.example.json — no embedded secrets | Placeholder values only, no real credentials | PASS |
| appsettings.example.json — AllowedHosts | Wildcard `*` | ADVISORY |
