# Security Audit: Phase 1

## Scope
7 new production files + Program.cs modification for multi-source API client infrastructure.

## Findings

### MEDIUM
- **API key transmitted via DefaultRequestHeaders** (`Program.cs:75`): `client.DefaultRequestHeaders.Add("X-Api-Key", source.ApiKey)` — this is the existing pattern from the single-source implementation. The key is sent on every request to the source. This is acceptable for server-to-server communication in Blazor Server (all HTTP calls are server-side). The key never reaches the browser.

### LOW
- **BaseUrl from config used directly as HttpClient.BaseAddress** (`Program.cs:73`): `client.BaseAddress = new Uri(source.BaseUrl)` — config-controlled. No user input reaches this path. SSRF risk is configuration-level only (admin misconfiguration), not exploitable by end users.
- **Error message in legacy config detection** (`DashboardConfigParser.cs:62-79`): The InvalidOperationException message includes example JSON format but no actual config values. No secrets leakage.

### INFO
- **ApiKey stored as plain string in DashboardApiSourceConfig** — this is the config binding pattern. The key originates from appsettings.json or environment variables. No encryption at rest beyond OS/config provider protections. Consistent with the existing `DashboardAuth:PasswordHash` pattern.
- **appsettings.json has empty Sources array** — no default secrets. Self-contained mode auto-adds Local source with no API key.
- **Slug derivation is deterministic and URL-safe** — no injection risk in URL segments. Slugs only contain `[a-z0-9-]`.

## Verdict
No CRITICAL or HIGH findings. Phase 1 code follows secure patterns. API keys are server-side only (Blazor Server), never exposed to browser. Config validation doesn't leak sensitive values.
