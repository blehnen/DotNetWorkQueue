# Serializers

Message bodies are serialized by whatever is registered as `ISerializer`. The default is
`JsonSerializer`, which uses Newtonsoft.Json, and **it stays the default deliberately**: callers can
queue any POCO they like, and Newtonsoft handles a wider range of shapes with no annotation than the
alternatives do.

`SystemTextJsonSerializer` is available for callers who own their message types and want the
allocation back.

## What it is worth

Measured on net10 by `Source/DotNetWorkQueue.Benchmarks`, for a message whose body is a 256-byte
string (the serialized form is larger - it also carries the wrapper and the type name):

| | Newtonsoft | System.Text.Json |
|---|---|---|
| serialize | 682 ns / 4,464 B | 270 ns / 752 B |
| deserialize | 768 ns / 4,552 B | 407 ns / 1,272 B |

About 7 KB less garbage per message round trip. The send half alone is roughly a fifth of what an
entire SQLite send allocates.

## Opting in

Register it during transport initialization:

```csharp
using var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(serviceRegister =>
    serviceRegister.Register<ISerializer, SystemTextJsonSerializer>(LifeStyles.Singleton));
```

That changes what **writes** new messages. Reading is covered in the next section, and on a queue
that already holds messages you have to deal with it.

## Reading messages back: the serializer marker

Every message carries a `Queue-SerializerId` header naming the serializer that wrote its body. The
consumer reads the headers before the body — they already carry the interceptor graph — so by the
time a body is deserialized the right serializer is known. `ISerializerResolver` does the lookup,
and no transport is involved: the resolution happens in `RootSerializer`, which every transport
reaches deserialization through.

This means a queue can hold messages written by more than one serializer at once, which is what
makes a migration possible at all.

### Messages written before the marker existed

Anything enqueued before this feature shipped carries no `Queue-SerializerId` header. Those fall
back to `ISerializerResolver.Fallback`, which defaults to the serializer registered for the queue —
exactly the behaviour that applied before the header existed, where whatever was registered read
everything.

**If you change the serializer on a queue that already holds messages, you must point the fallback
at whatever wrote them**, or they become unreadable:

```csharp
using var queueContainer = new QueueContainer<SqLiteMessageQueueInit>(serviceRegister =>
{
    var binder = new DenyListSerializationBinder();

    // what writes new messages
    serviceRegister.Register<ISerializer>(() => new SystemTextJsonSerializer(binder),
        LifeStyles.Singleton);

    // what reads them back, including the ones already in the queue
    serviceRegister.Register<ISerializerResolver>(() =>
    {
        var resolver = new SerializerResolver(new SystemTextJsonSerializer(binder));
        resolver.SetFallback(new JsonSerializer(binder));   // wrote the existing backlog
        return resolver;
    }, LifeStyles.Singleton);
});
```

A message naming a serializer that is not registered throws rather than falling back. Reading a body
with the wrong serializer does not reliably fail — it can hand back a half-populated object — and a
poison message is far easier to diagnose than silent data loss.

### Draining a backlog

The safe order is: teach the consumers to read the new format first, then switch the producers.

1. Deploy consumers that register both serializers, still writing with the old one.
2. Switch the producers to the new serializer. Consumers read both, selecting per message.
3. Once the backlog of unmarked messages has drained, the fallback stops mattering.

## Limitations of the System.Text.Json serializer

Both come from System.Text.Json itself rather than from this library.

**A property declared as a concrete base class, holding a derived instance, loses the derived
part.** Newtonsoft writes a type marker whenever the runtime type differs from the declared one.
System.Text.Json needs the derived types declared up front:

```csharp
[JsonDerivedType(typeof(Animal), "animal")]
[JsonDerivedType(typeof(Dog), "dog")]
public class Animal { }
public class Dog : Animal { }
```

Properties declared as `object`, as an interface, or as an abstract class need no annotation — those
are handled and are covered by tests.

**Properties with private setters are not restored.** The Newtonsoft *message* serializer does not
restore them either, so this matches existing behaviour rather than changing it.

## Security

`SystemTextJsonSerializer` resolves types through the same `ISerializationBinder` the Newtonsoft
path uses, so the configured allow or deny list governs both identically. Replacing
`DenyListSerializationBinder` with `AllowListSerializationBinder` affects both.

## What is not pluggable

`IInternalSerializer` — which writes the headers — stays on Newtonsoft. The serializer marker lives
in the headers, so making the header envelope itself pluggable would mean needing to know how the
headers were written in order to read how they were written.

The mechanics of a System.Text.Json header path were prototyped and do work, so the obstacle is
versioning rather than feasibility. Two converters are needed: System.Text.Json refuses
`System.Type` outright (`NotSupportedException` on `System.RuntimeType`, a deliberate decision on
their part), and `MessageInterceptorsGraph` exposes a getter-only `IEnumerable<Type>` that it will
not populate. With both supplied, the headers round-trip identically to Newtonsoft.

`IExpressionSerializer`, used by the LINQ and method queues, is a separate registration and is
unaffected by any of this.
