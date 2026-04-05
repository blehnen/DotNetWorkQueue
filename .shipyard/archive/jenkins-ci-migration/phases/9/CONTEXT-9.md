# Phase 9 (Tier B) — Design Decisions

## Health Check Endpoint
- **Decision:** Basic health check only — return 200 OK with uptime and service name
- **Rationale:** No database connectivity checks. Keeps it fast and dependency-free. Monitoring tools just need a liveness probe.

## Dashboard API Deployment Model
- **Decision:** Recommended internal-only (behind VPN). No HTTPS redirect or rate limiting in the API itself.
- **Rationale:** HTTPS and rate limiting are infrastructure concerns handled by reverse proxy/load balancer.

## CORS Configuration
- **Decision:** Add configurable CORS policy to DashboardOptions. Default to empty (no CORS headers). Users configure allowed origins when Blazor UI is on a different origin.

## DashboardExtensions.cs Contention
- **Decision:** Task 2 (Exception Filter + CORS) owns DashboardExtensions.cs and includes the health check service registration. Task 3 only creates HealthController and README.

## Central Package Management
- **Decision:** Use Directory.Packages.props with ManagePackageVersionsCentrally. Extract versions programmatically from existing .csproj files to ensure nothing is missed.
