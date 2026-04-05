# Dashboard UI Polish Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use shipyard:shipyard-executing-plans to implement this plan task-by-task.

**Goal:** Tighten UI density on Connections and Queue list pages, remove the nav drawer in favor of breadcrumbs and a clickable title.

**Architecture:** Pure Razor/MudBlazor component changes across 4 files. No API, backend, or model changes. Replace MudCard grids with MudSimpleTable for compact layout. Remove MudDrawer/NavMenu and make the app bar title a clickable link home.

**Tech Stack:** Blazor Server, MudBlazor 9.1.0, C#

---

<task id="1" name="Remove Nav Drawer and NavMenu">
  <description>Remove the MudDrawer, hamburger toggle button, and NavMenu component from MainLayout.razor. Delete NavMenu.razor. Make the app bar title a clickable link back to the Connections page.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/MainLayout.razor:14-27</modify>
    <delete>Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/NavMenu.razor</delete>
  </files>
  <steps>
    <step>Delete NavMenu.razor</step>
    <step>Modify MainLayout.razor to remove drawer and hamburger</step>
    <step>Build to verify</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</command>
    <expected>Build succeeded. 0 Error(s)</expected>
  </verification>
</task>

### Task 1: Remove Nav Drawer and NavMenu

**Files:**
- Delete: `Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/NavMenu.razor`
- Modify: `Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/MainLayout.razor`

**Step 1: Delete NavMenu.razor**

Delete the file `Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/NavMenu.razor` entirely (5 lines, single MudNavLink).

**Step 2: Modify MainLayout.razor**

Replace lines 13-31 (the MudLayout content) with this version that removes the drawer, hamburger button, and NavMenu reference. The title becomes a clickable link:

```razor
    <MudLayout>
        <MudAppBar Elevation="1">
            <MudLink Href="/" Underline="Underline.None" Color="Color.Inherit">
                <MudText Typo="Typo.h6">DotNetWorkQueue Dashboard</MudText>
            </MudLink>
            <MudSpacer />
            @if (AuthConfig.IsEnabled)
            {
                <MudIconButton Icon="@Icons.Material.Filled.Logout" Color="Color.Inherit"
                               title="Sign out" OnClick="Logout" />
            }
        </MudAppBar>
        <MudMainContent Class="pa-4">
            @Body
        </MudMainContent>
    </MudLayout>
```

Also remove from the `@code` block:
- Line 35: `private bool _drawerOpen = true;` — no longer needed
- Line 73: `private void ToggleDrawer() => _drawerOpen = !_drawerOpen;` — no longer needed

**Step 3: Build to verify**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"`
Expected: `Build succeeded. 0 Error(s)`

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/MainLayout.razor
git rm Source/DotNetWorkQueue.Dashboard.Ui/Components/Layout/NavMenu.razor
git commit -m "ui: remove nav drawer and NavMenu, make title clickable"
```

---

<task id="2" name="Compact Connections Table">
  <description>Replace the MudGrid/MudCard layout on Home.razor with a MudSimpleTable. Each row shows the connection display name, queue count, and a storage icon. Rows are clickable.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor:6-51</modify>
  </files>
  <steps>
    <step>Replace card grid with table markup</step>
    <step>Build to verify</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"</command>
    <expected>Build succeeded. 0 Error(s)</expected>
  </verification>
</task>

### Task 2: Compact Connections Table on Home.razor

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor`

**Step 1: Replace card grid with compact table**

Replace lines 6-51 (the heading through the closing `}` of the else block) with:

```razor
<MudBreadcrumbs Items="@(new List<BreadcrumbItem> { new BreadcrumbItem("Connections", href: null, disabled: true) })" Class="mb-2" />

@if (_loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (_error != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-4">
        Failed to load connections: @_error
    </MudAlert>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="LoadConnections">
        Retry
    </MudButton>
}
else if (_connections == null || _connections.Count == 0)
{
    <MudAlert Severity="Severity.Info">
        No connections registered. Configure connections in the Dashboard API.
    </MudAlert>
}
else
{
    <MudSimpleTable Elevation="2" Dense="true" Hover="true" Class="cursor-pointer">
        <thead>
            <tr>
                <th>Connection</th>
                <th>Queues</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var connection in _connections)
            {
                <tr @onclick="() => NavigateToConnection(connection.Id)">
                    <td>
                        <MudIcon Icon="@Icons.Material.Filled.Storage" Size="Size.Small" Color="Color.Primary" Class="mr-2" />
                        @connection.DisplayName
                    </td>
                    <td>@connection.QueueCount</td>
                </tr>
            }
        </tbody>
    </MudSimpleTable>
}
```

**Step 2: Build to verify**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"`
Expected: `Build succeeded. 0 Error(s)`

**Step 3: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/Home.razor
git commit -m "ui: replace connection cards with compact table"
```

---

<task id="3" name="Compact Queue List Table">
  <description>Replace the MudGrid/MudCard layout for queues on ConnectionDetail.razor with a MudSimpleTable. Each row shows queue name and consumer count. Rows are clickable. Keep the Scheduled Jobs section unchanged.</description>
  <files>
    <modify>Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor:20-54</modify>
  </files>
  <steps>
    <step>Replace queue card grid with table markup</step>
    <step>Build to verify</step>
    <step>Run Dashboard integration tests</step>
    <step>Commit</step>
  </steps>
  <verification>
    <command>dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory" -f net10.0</command>
    <expected>Passed!</expected>
  </verification>
</task>

### Task 3: Compact Queue List Table on ConnectionDetail.razor

**Files:**
- Modify: `Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor`

**Step 1: Replace queue card grid with compact table**

Replace lines 20-54 (the "Queues" heading through the closing of the queue grid) with:

```razor
    <MudText Typo="Typo.h5" Class="mb-2">Queues</MudText>

    @if (_queues == null || _queues.Count == 0)
    {
        <MudAlert Severity="Severity.Info" Class="mb-4">No queues found for this connection.</MudAlert>
    }
    else
    {
        <MudSimpleTable Elevation="2" Dense="true" Hover="true" Class="mb-6 cursor-pointer">
            <thead>
                <tr>
                    <th>Queue</th>
                    <th>Consumers</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var queue in _queues)
                {
                    <tr @onclick="() => NavigateToQueue(queue)">
                        <td>
                            <MudIcon Icon="@Icons.Material.Filled.Queue" Size="Size.Small" Color="Color.Primary" Class="mr-2" />
                            @queue.QueueName
                        </td>
                        <td>
                            @{
                                var count = GetConsumerCount(queue.Id);
                            }
                            @if (count > 0)
                            {
                                <MudChip T="string" Color="Color.Success" Variant="Variant.Outlined" Size="Size.Small">
                                    @count
                                </MudChip>
                            }
                            else
                            {
                                <MudText Typo="Typo.body2" Color="Color.Secondary">0</MudText>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </MudSimpleTable>
    }
```

Keep everything else unchanged — the Scheduled Jobs section (lines 56+), the `@code` block, breadcrumbs, and all methods remain as-is.

Also change the Scheduled Jobs heading from `Typo.h4` to `Typo.h5` for consistency:

```razor
    <MudText Typo="Typo.h5" Class="mb-2">Scheduled Jobs</MudText>
```

**Step 2: Build to verify**

Run: `dotnet build "Source/DotNetWorkQueue.Dashboard.Ui/DotNetWorkQueue.Dashboard.Ui.csproj"`
Expected: `Build succeeded. 0 Error(s)`

**Step 3: Run Dashboard integration tests**

Run: `dotnet test "Source/DotNetWorkQueue.Dashboard.Api.Integration.Tests/DotNetWorkQueue.Dashboard.Api.Integration.Tests.csproj" --filter "FullyQualifiedName~Memory" -f net10.0`
Expected: `Passed!` (API endpoints unaffected by UI changes)

**Step 4: Commit**

```bash
git add Source/DotNetWorkQueue.Dashboard.Ui/Components/Pages/ConnectionDetail.razor
git commit -m "ui: replace queue cards with compact table"
```
