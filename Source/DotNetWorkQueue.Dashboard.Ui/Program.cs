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
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DotNetWorkQueue.Dashboard.Api;
using DotNetWorkQueue.Dashboard.Ui.Components;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// --- Self-contained mode: embed Dashboard API in this process ---
var dashboardSection = builder.Configuration.GetSection("Dashboard");
var selfContained = dashboardSection.GetSection("Connections").GetChildren().Any();
if (selfContained)
{
    builder.Services.AddDotNetWorkQueueDashboard(dashboardSection);
}

// --- Multi-source API client registration ---
DashboardConfigParser.ValidateNoLegacyConfig(builder.Configuration);
var sources = DashboardConfigParser.ParseSources(builder.Configuration);

// In self-contained mode, auto-add a "Local" source if one is not already configured
if (selfContained && sources.All(s => !string.Equals(s.Name, "Local", StringComparison.OrdinalIgnoreCase)))
{
    var localName = builder.Configuration["DashboardApi:LocalSourceName"] ?? "Local";
    sources.Add(new DashboardApiSourceConfig { Name = localName, BaseUrl = "http://localhost:5000" });
    builder.Services.AddHostedService<LocalSourceHostedService>();
}

// Default fallback: if no sources configured at all, add a default Local source
if (sources.Count == 0)
{
    sources.Add(new DashboardApiSourceConfig { Name = "Local", BaseUrl = "http://localhost:5000" });
}

var registry = new SourceRegistry(sources);
builder.Services.AddSingleton<ISourceRegistry>(registry);

foreach (var source in sources)
{
    builder.Services.AddHttpClient(source.Slug, client =>
    {
        client.BaseAddress = new Uri(source.BaseUrl);
        if (!string.IsNullOrEmpty(source.ApiKey))
            client.DefaultRequestHeaders.Add("X-Api-Key", source.ApiKey);
    });
}

builder.Services.AddSingleton<IMultiSourceDashboardApiClient, MultiSourceDashboardApiClient>();

// --- Authentication ---
var authUsername = builder.Configuration["DashboardAuth:Username"] ?? "";
var authPasswordHash = builder.Configuration["DashboardAuth:PasswordHash"] ?? "";
var authEnabled = authUsername.Length > 0 && authPasswordHash.Length > 0;

var authConfig = new DashboardAuthConfig
{
    IsEnabled = authEnabled,
    Username = authUsername,
    PasswordHash = authPasswordHash
};
builder.Services.AddSingleton(authConfig);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

if (!string.IsNullOrEmpty(authUsername) && string.IsNullOrEmpty(authPasswordHash))
{
    Console.WriteLine();
    Console.WriteLine("WARNING: DashboardAuth:Username is set but DashboardAuth:PasswordHash is empty.");
    Console.WriteLine("Authentication is DISABLED until a password hash is configured.");
    Console.WriteLine();
    Console.WriteLine("To generate a SHA256 password hash:");
    Console.WriteLine("  PowerShell:  $s=[Security.Cryptography.SHA256]::Create(); [BitConverter]::ToString($s.ComputeHash([Text.Encoding]::UTF8.GetBytes('yourpassword'))).Replace('-','').ToLower()");
    Console.WriteLine("  Bash:        echo -n 'yourpassword' | sha256sum | cut -d' ' -f1");
    Console.WriteLine();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

if (selfContained)
{
    app.UseDotNetWorkQueueDashboard();
}

app.UseRouting();
app.UseAuthentication();
app.UseAntiforgery();

app.UseStaticFiles();

if (selfContained)
{
    app.MapControllers();
}

// --- Login / Logout endpoints ---
app.MapPost("/auth/login", async (HttpContext ctx) =>
{
    var form = await ctx.Request.ReadFormAsync().ConfigureAwait(false);
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

    if (string.Equals(username, authConfig.Username, StringComparison.OrdinalIgnoreCase)
        && string.Equals(hash, authConfig.PasswordHash, StringComparison.OrdinalIgnoreCase))
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity)).ConfigureAwait(false);
        ctx.Response.Redirect("/");
    }
    else
    {
        ctx.Response.Redirect("/login?error=1");
    }
});

app.MapGet("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    ctx.Response.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
