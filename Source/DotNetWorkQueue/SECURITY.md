# DotNetWorkQueue security guide

## Overview

DotNetWorkQueue processes user-defined message payloads: POCOs and LINQ expressions. Both involve deserializing type-annotated JSON, and the expressions are compiled and run on the consumer. If you're deploying this in production, you need to understand these attack surfaces.

This document covers deserialization, dynamic code compilation, queue backend access control, the Dashboard API, and deployment recommendations.

## Deserialization

DotNetWorkQueue uses Newtonsoft.Json with `TypeNameHandling.Auto` to serialize and deserialize messages. This embeds .NET type names in JSON so polymorphic types round-trip correctly. The downside: without a binder, an attacker who can write to the queue backend could craft a payload that instantiates a dangerous type during deserialization, giving them remote code execution.

### Default protection: DenyListSerializationBinder

The library ships `DotNetWorkQueue.Serialization.DenyListSerializationBinder` as the default `ISerializationBinder`. It blocks 30 known deserialization gadget types:

- `System.Diagnostics.Process`
- `System.Windows.Data.ObjectDataProvider`
- `System.Security.Principal.WindowsIdentity`
- `System.Management.Automation.PSObject`
- `System.Runtime.Serialization.Formatters.Binary.BinaryFormatter`
- `System.Windows.Markup.XamlReader`
- `System.Data.DataSet` / `System.Data.DataTable`
- And 23 others (remoting channels, XAML, WCF, SOAP formatters)

A denied type throws `JsonSerializationException` with a message identifying what was blocked.

### Extending the deny list

If new gadget types are discovered, add them at startup before any deserialization happens:

```csharp
// Add a single type
binder.AddDeniedType("Some.Dangerous.TypeName");

// Add multiple types
binder.AddDeniedTypes(new[] {
    "Some.Dangerous.TypeA",
    "Some.Dangerous.TypeB"
});
```

These methods are not thread-safe with concurrent `BindToType` calls. Call them during startup only.

### Maximum lockdown: AllowListSerializationBinder

For environments that need strict control, replace the default binder with `DotNetWorkQueue.Serialization.AllowListSerializationBinder`. This blocks all types by default. You must register everything you want to deserialize:

```csharp
var binder = new AllowListSerializationBinder();

// Register by fully qualified type name
binder.AddAllowedType("MyApp.Messages.OrderMessage");

// Register by Type reference
binder.AddAllowedType(typeof(MyApp.Messages.OrderMessage));

// Register multiple types
binder.AddAllowedTypes(new[] {
    "MyApp.Messages.OrderMessage",
    "MyApp.Messages.ShipmentMessage"
});
```

Anything not on the list throws `JsonSerializationException`.

### Replacing the binder via DI

To switch to the allow-list binder (or a custom one), register it during queue setup:

```csharp
using DotNetWorkQueue.Serialization;
using Newtonsoft.Json.Serialization;

var container = new QueueContainer<MyTransportInit>(container =>
{
    container.Register<ISerializationBinder, AllowListSerializationBinder>(LifeStyles.Singleton);
});
```

If neither the deny-list nor allow-list binder fits your needs, implement your own `ISerializationBinder` with whatever policy logic you require and register it the same way.

## Method queues and LINQ expressions

### How it works

The method queues (`IProducerMethodQueue`, `IConsumerMethodQueue`) send a LINQ expression rather
than a POCO. The producer serializes an `Expression<Action<...>>` to JSON, and the consumer
reconstructs it, compiles it, and runs it.

### What this means

An expression tree is only as trustworthy as the message it was rebuilt from. The consumer compiles
whatever the JSON describes, so anyone who can write to the queue backend can influence what runs
on a consumer, the same trust boundary as the deserialization concern above, and with the same
consequence.

This applies on every target. There is no framework where a method queue consumer executes only
what its own producers sent.

### Mitigations

- If you do not need method queues, use the POCO queues (`IProducerQueue<T>`, `IConsumerQueue`).
  They are still bound by the deserialization concern above, but nothing compiles and invokes a
  caller-supplied expression.
- If you do need them, control who can write to the queue backend. That is the boundary that
  matters; see the next section.

## Queue backend access control

The queue backend (SQL Server, PostgreSQL, SQLite, Redis, LiteDB, or Memory) is the trust boundary. Anyone who can write to it can inject malicious payloads for deserialization attacks, supply an expression for a method queue consumer to compile and run, or corrupt queue metadata.

Recommendations:

- Authenticate all connections. Use database credentials, Redis AUTH, or equivalent. Never expose queue backends with anonymous access.
- Restrict which hosts can connect. Only trusted producer and consumer hosts should reach the queue backend. Use firewalls, security groups, or network policies.
- Use TLS. SQL Server (`Encrypt=true`), PostgreSQL (`SSL Mode=Require`), and Redis all support encrypted connections.
- Use least-privilege database accounts. Consumers need read/write/delete on queue tables. Producers need insert. Neither needs DDL after initial schema creation.
- Isolate queue databases. Don't co-locate queue tables in a shared application database where other users or services have write access.

## Dashboard API

The optional Dashboard API (`DotNetWorkQueue.Dashboard.Api`) exposes queue status, consumer tracking, and maintenance endpoints over HTTP. It has two built-in security filters:

**API key authentication.** `ApiKeyAuthorizationFilter` validates the `X-Api-Key` header against `DashboardOptions.ApiKey`. When configured, requests without a valid key get `401 Unauthorized`. When not configured, the filter is inactive.

**Read-only mode.** `ReadOnlyFilter` restricts the API to read-only operations when enabled.

**Custom authorization policy.** Set `DashboardOptions.AuthorizationPolicy` to the name of an ASP.NET Core authorization policy. The dashboard will apply that policy to all its controllers via `AuthorizeFilter`:

```csharp
// Register your policy in Program.cs / Startup.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DashboardAdmins", policy =>
        policy.RequireRole("Admin"));
});

// Reference it in dashboard options
builder.Services.AddDotNetWorkQueueDashboard(options =>
{
    options.AuthorizationPolicy = "DashboardAdmins";
    // ... other options
});
```

Since the Dashboard API is standard ASP.NET Core, you can also add your own authentication middleware or custom filters beyond what's built in.

Recommendations:

- Always set `DashboardOptions.ApiKey` in production.
- Serve over HTTPS only. The API key travels in headers and must be encrypted in transit.
- Restrict network access. Bind to internal networks or use a reverse proxy with IP allowlists.
- Rotate the API key periodically.
- Enable read-only mode unless you need state-changing operations through the dashboard.

## Deployment checklist

1. **Use the deny-list binder (default) or switch to the allow-list binder.** The deny-list blocks 30 known gadget types out of the box. The allow-list is stricter but requires you to register every type you deserialize.

2. **Restrict queue backend write access to trusted producers.** Database authentication plus network ACLs. This is the single most effective mitigation against payload injection.

3. **Skip method queues if you do not need them.** Use `IProducerQueue<T>` / `IConsumerQueue` for POCO messages. Nothing then compiles and runs an expression that arrived in a message.

4. **Monitor queue message patterns.** Unexpected message types, unfamiliar producers, or unusual payload sizes can indicate injection.

5. **Keep DotNetWorkQueue updated.** New gadget types get discovered. Updates to the deny list ship in new releases.

6. **Use TLS for all backend connections.** `Encrypt=true` (SQL Server), `SSL Mode=Require` (PostgreSQL), or TLS (Redis).

7. **Run consumers with least-privilege OS accounts.** If an attacker does get code execution, through a deserialization gadget or a method queue expression, a restricted service account limits the damage.

## Reporting security issues

If you find a security vulnerability, please report it through [GitHub Issues](https://github.com/blehnen/DotNetWorkQueue/issues). Put "SECURITY" in the title so it gets attention quickly.

Include: what the vulnerability is, how to reproduce it, which version(s) are affected, and any suggested fix.
