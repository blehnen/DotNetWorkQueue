# Config-Driven Transport Registration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Move the JSON-driven transport registration pattern from the sample project into the base Dashboard API library, enabling config-file-only Dashboard setup (required for Docker).

**Architecture:** Add transport project references to Dashboard.Api csproj, create a config POCO for JSON binding, add a transport resolver method and an IConfiguration-based overload of AddDotNetWorkQueueDashboard. The existing Action<DashboardOptions> overload stays unchanged.

**Tech Stack:** ASP.NET Core, Microsoft.Extensions.Configuration, C#

---

<task id="1" name="Add Transport References and Config POCO">
  <description>Add ProjectReferences for all 5 transport projects to Dashboard.Api.csproj. Create DashboardConnectionConfig POCO for JSON binding.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj:49-52</modify>
    <create>Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardConnectionConfig.cs</create>
  </files>
  <steps>
    <step>Add transport ProjectReferences to csproj</step>
    <step>Create DashboardConnectionConfig POCO</step>
    <step>Build to verify</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Debug</command>
    <expected>Build succeeded. 0 Error(s)</expected>
  </verification>
</task>

### Task 1: Add Transport References and Config POCO

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj`
- Create: `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardConnectionConfig.cs`

**Step 1: Add transport ProjectReferences to csproj**

Add these ProjectReferences to the existing ItemGroup (lines 49-52) that already has DotNetWorkQueue and Transport.RelationalDatabase:

```xml
  <ItemGroup>
    <ProjectReference Include="..\DotNetWorkQueue\DotNetWorkQueue.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.RelationalDatabase\DotNetWorkQueue.Transport.RelationalDatabase.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.SqlServer\DotNetWorkQueue.Transport.SqlServer.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.PostgreSQL\DotNetWorkQueue.Transport.PostgreSQL.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.Redis\DotNetWorkQueue.Transport.Redis.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.SQLite\DotNetWorkQueue.Transport.SQLite.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.LiteDB\DotNetWorkQueue.Transport.LiteDB.csproj" />
    <ProjectReference Include="..\DotNetWorkQueue.Transport.Shared\DotNetWorkQueue.Transport.Shared.csproj" />
  </ItemGroup>
```

**Step 2: Create DashboardConnectionConfig POCO**

Create `Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardConnectionConfig.cs`:

```csharp
// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// Represents a single transport connection entry in the Dashboard JSON configuration.
    /// </summary>
    public class DashboardConnectionConfig
    {
        /// <summary>
        /// The transport type name: "SqlServer", "PostgreSql", "SQLite", "LiteDb", or "Redis".
        /// </summary>
        public string Transport { get; set; } = string.Empty;

        /// <summary>
        /// The connection string for this transport.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Optional display name shown in the Dashboard UI. Defaults to the transport name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Queue names to monitor on this connection.
        /// </summary>
        public string[] Queues { get; set; } = System.Array.Empty<string>();
    }
}
```

**Step 3: Build to verify**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Debug`
Expected: `Build succeeded. 0 Error(s)`

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj
git add Source/DotNetWorkQueue.Dashboard.Api/Configuration/DashboardConnectionConfig.cs
git commit -m "feat: add transport references and DashboardConnectionConfig POCO

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

<task id="2" name="Add IConfiguration Overload with Transport Resolution">
  <description>Add a transport resolver method and an IConfiguration-based overload of AddDotNetWorkQueueDashboard to DashboardExtensions.cs. The overload reads Dashboard:Connections[] from config, resolves transport names to ITransportInit types, and registers each connection.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs:19-31,138</modify>
  </files>
  <steps>
    <step>Add using directives for transport namespaces and IConfiguration</step>
    <step>Add IConfiguration overload method</step>
    <step>Add transport resolver method</step>
    <step>Build to verify</step>
    <step>Run Dashboard integration tests</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory" -f net10.0</command>
    <expected>Passed!</expected>
  </verification>
</task>

### Task 2: Add IConfiguration Overload with Transport Resolution

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs`

**Step 1: Add using directives**

Add these usings after the existing using block (after line 31):

```csharp
using Microsoft.Extensions.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.LiteDb.Basic;
```

**Step 2: Add IConfiguration overload**

Add this method after the existing `AddDotNetWorkQueueDashboard` method (after line 138, before `UseDotNetWorkQueueDashboard`):

```csharp
        /// <summary>
        /// Adds DotNetWorkQueue Dashboard services configured from an IConfiguration section.
        /// Reads Dashboard:Connections[] entries and resolves transport types by name.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="dashboardSection">The "Dashboard" configuration section containing Connections, EnableSwagger, ApiKey, etc.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDotNetWorkQueueDashboard(
            this IServiceCollection services,
            IConfiguration dashboardSection)
        {
            var interceptorOptions = dashboardSection.GetSection("Interceptors")
                .Get<DashboardInterceptorOptions>();

            return services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = dashboardSection.GetValue("EnableSwagger", true);
                options.ApiKey = dashboardSection.GetValue<string>("ApiKey") ?? string.Empty;

                foreach (var conn in dashboardSection.GetSection("Connections").GetChildren())
                {
                    var transport = conn["Transport"];
                    var connectionString = conn["ConnectionString"];
                    var displayName = conn["DisplayName"] ?? transport;
                    var queues = conn.GetSection("Queues").Get<string[]>() ?? Array.Empty<string>();

                    if (string.IsNullOrEmpty(transport))
                        throw new ArgumentException("Each Dashboard connection must specify a Transport.");
                    if (string.IsNullOrEmpty(connectionString))
                        throw new ArgumentException($"Dashboard connection '{displayName}' must specify a ConnectionString.");

                    AddConnectionByTransport(options, transport, connectionString, displayName!, queues, interceptorOptions);
                }
            });
        }
```

**Step 3: Add transport resolver method**

Add this private static method at the end of the `DashboardExtensions` class (before the closing brace of the class, after the `UseDotNetWorkQueueDashboard` method):

```csharp
        private static void AddConnectionByTransport(DashboardOptions options, string transport,
            string connectionString, string displayName, string[] queues,
            DashboardInterceptorOptions interceptors)
        {
            switch (transport)
            {
                case "SqlServer":
                    options.AddConnection<SqlServerMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "PostgreSql":
                    options.AddConnection<PostgreSqlMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "SQLite":
                    options.AddConnection<SqLiteMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "LiteDb":
                    options.AddConnection<LiteDbMessageQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                case "Redis":
                    options.AddConnection<RedisQueueInit>(connectionString, conn =>
                    {
                        conn.DisplayName = displayName;
                        foreach (var queue in queues)
                            conn.AddQueue(queue, interceptors);
                    });
                    break;
                default:
                    throw new ArgumentException($"Unknown transport type: '{transport}'. Valid values: SqlServer, PostgreSql, SQLite, LiteDb, Redis.");
            }
        }
```

**Step 4: Build to verify**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Api/DotNetWorkQueue.Dashboard.Api.csproj" -c Debug`
Expected: `Build succeeded. 0 Error(s)`

**Step 5: Run Dashboard integration tests**

Run: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory" -f net10.0`
Expected: `Passed!` (existing fluent API path unchanged)

**Step 6: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Api/DashboardExtensions.cs
git commit -m "feat: add IConfiguration overload for JSON-driven transport registration

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```
