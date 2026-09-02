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
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Memory;
using MemoryBasic = DotNetWorkQueue.Transport.Memory.Basic;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a send that has no transport cost in it, so what is left is the core library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Memory transport stores the POCO in a dictionary — no serialization, no SQL, no I/O —
    /// so every microsecond and every byte measured here is the library's own pipeline, and a
    /// change to any of it moves every transport at once. <c>SendPathBenchmarks</c> already
    /// carries the end-to-end number as one rung; this class takes that rung apart.
    /// </para>
    /// <para>
    /// Read it as a ladder. The difference between two adjacent rungs is the cost of what the
    /// upper one adds:
    /// </para>
    /// <list type="number">
    /// <item><description><see cref="RawStore"/> — the floor: what the two collections the
    /// transport writes to cost on their own.</description></item>
    /// <item><description><see cref="DataStorage_Send"/> — the transport's storage layer.
    /// </description></item>
    /// <item><description><see cref="DataStorage_SendTraced"/> — the same through its trace
    /// decorator.</description></item>
    /// <item><description><see cref="SendMessages_Undecorated"/> — the transport's
    /// <see cref="ISendMessages"/>.</description></item>
    /// <item><description><see cref="SendMessages_Decorated"/> — the same through the policy,
    /// history and metrics decorators the container puts around it.</description></item>
    /// <item><description><see cref="Producer_Send"/> — the whole thing, as a caller sees it.
    /// Minus the row above, this is header generation, the message factory and the standard
    /// headers, which the three component rungs below then split up.</description></item>
    /// </list>
    /// <para>
    /// Every rung runs against a queue created fresh for the iteration, and each iteration is a
    /// single invocation of <see cref="Sends"/> operations. That matters: the Memory store is a
    /// process-wide static that nothing drains here, so letting BenchmarkDotNet choose the
    /// invocation count would have later iterations writing into a dictionary holding millions of
    /// entries and would measure that growth as if it were send cost.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(1)]
    public class MemoryPathBenchmarks
    {
        /// <summary>Operations per invocation. Also the deepest the store gets in one iteration.</summary>
        private const int Sends = 50000;
        private const int PayloadBytes = 256;

        private string _payload;

        private QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit> _creation;
        private QueueContainer<MemoryBasic.MemoryMessageQueueInit> _container;
        private IProducerQueue<Event> _producer;

        //pulled out of the live producer so the rungs measure the configured instances rather than
        //a hand-built approximation that could drift from the real send path
        private ISendMessages _sendDecorated;
        private ISendMessages _sendUndecorated;
        private IDataStorageSendMessage _storageTraced;
        private IDataStorageSendMessage _storage;
        private IMessageFactory _messageFactory;
        private GenerateMessageHeaders _generateHeaders;
        private AddStandardMessageHeaders _addStandardHeaders;

        //a message and its data, prepared once, for the rungs below the producer
        private IMessage _preparedMessage;
        private IAdditionalMessageData _preparedData;

        //the raw-store floor
        private ConcurrentDictionary<Guid, object> _rawData;
        private System.Collections.Concurrent.BlockingCollection<Guid> _rawQueue;

        private List<Event> _batch;

        //every constructed object is stored, so none of them can be elided as non-escaping - the
        //JIT will stack-allocate an object it can prove never leaves the method, and the lazy
        //shape below is simple enough for it to do exactly that
        private object[] _sink;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _payload = new string('x', PayloadBytes);
            _sink = new object[Sends];
            _batch = new List<Event>(Sends);
            for (var i = 0; i < Sends; i++)
                _batch.Add(new Event { Body = _payload });
        }

        [IterationSetup]
        public void IterationSetup()
        {
            var connection = new QueueConnection("benchMemoryPath" + Guid.NewGuid().ToString("N"), "memory");
            _creation = new QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<MemoryBasic.MessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"memory CreateQueue failed: {result.Status}");
            }

            _container = new QueueContainer<MemoryBasic.MemoryMessageQueueInit>();
            _producer = _container.CreateProducer<Event>(connection);

            _sendDecorated = (ISendMessages)Field(_producer, "_sendMessages");
            _sendUndecorated = (ISendMessages)Innermost(_sendDecorated, "_dataStorage");
            _storageTraced = (IDataStorageSendMessage)Field(_sendUndecorated, "_dataStorage");
            _storage = (IDataStorageSendMessage)Innermost(_storageTraced, "_connectionInformation");

            _messageFactory = (IMessageFactory)Field(_producer, "_messageFactory");
            _generateHeaders = (GenerateMessageHeaders)Field(_producer, "_generateMessageHeaders");
            _addStandardHeaders = (AddStandardMessageHeaders)Field(_producer, "_addStandardMessageHeaders");

            _preparedData = new AdditionalMessageData();
            _preparedMessage = _messageFactory.Create(new Event { Body = _payload },
                _generateHeaders.HeaderSetup(_preparedData));
            _addStandardHeaders.AddHeaders(_preparedMessage, _preparedData);

            _rawData = new ConcurrentDictionary<Guid, object>();
            _rawQueue = new System.Collections.Concurrent.BlockingCollection<Guid>();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            //the store is a process-wide static keyed by connection; without this the iterations
            //would accumulate and the process would grow without bound
            (_storage as IDataStorage)?.Clear();
            _producer?.Dispose();
            _container?.Dispose();
            _creation?.Dispose();
            _rawQueue?.Dispose();

            //an iteration leaves tens of thousands of live objects in the store; collecting them
            //here rather than letting them die during the next measurement keeps a GC out of the
            //timings. Cleanup is not measured.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// The floor: the two collections the Memory transport writes to, and the id it generates,
        /// with nothing else.
        /// </summary>
        [Benchmark(Baseline = true, OperationsPerInvoke = Sends, Description = "raw store: dictionary + queue add")]
        public void RawStore()
        {
            for (var i = 0; i < Sends; i++)
            {
                var id = Guid.NewGuid();
                _rawData.TryAdd(id, _payload);
                _rawQueue.Add(id);
            }
        }

        /// <summary>The transport's storage layer, called with an already-prepared message.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "DataStorage.SendMessage")]
        public void DataStorage_Send()
        {
            for (var i = 0; i < Sends; i++)
                _storage.SendMessage(_preparedMessage, _preparedData);
        }

        /// <summary>The same through the trace decorator the container wraps it in.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "DataStorage.SendMessage + trace decorator")]
        public void DataStorage_SendTraced()
        {
            for (var i = 0; i < Sends; i++)
                _storageTraced.SendMessage(_preparedMessage, _preparedData);
        }

        /// <summary>The transport's <see cref="ISendMessages"/>, with no decorators.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "ISendMessages, undecorated")]
        public void SendMessages_Undecorated()
        {
            for (var i = 0; i < Sends; i++)
                _sendUndecorated.Send(_preparedMessage, _preparedData);
        }

        /// <summary>The same through the policy, history and metrics decorators.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "ISendMessages, decorated (policy/history/metrics)")]
        public void SendMessages_Decorated()
        {
            for (var i = 0; i < Sends; i++)
                _sendDecorated.Send(_preparedMessage, _preparedData);
        }

        /// <summary>The whole send, as a caller experiences it.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "producer.Send (end to end)")]
        public void Producer_Send()
        {
            for (var i = 0; i < Sends; i++)
            {
                var result = _producer.Send(new Event { Body = _payload });
                if (result.HasError)
                    throw result.SendingException ?? new InvalidOperationException("send failed");
            }
        }

        /// <summary>
        /// The batch path, reported per message. The Memory transport has no bulk store, so this
        /// is the per-message loop plus whatever the batch shape itself costs.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "producer.Send(List) (end to end)")]
        public void Producer_SendBatch()
        {
            var results = _producer.Send(_batch);
            foreach (var result in results)
            {
                if (result.HasError)
                    throw result.SendingException ?? new InvalidOperationException("batch send failed");
            }
        }

        /// <summary>
        /// The batch shape the transport uses today: <see cref="Parallel.ForEach"/> writing into a
        /// <see cref="ConcurrentBag{T}"/>. Against the rung below it, this is what the parallelism
        /// is worth for a store that is already a concurrent dictionary.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "batch shape: Parallel.ForEach into a ConcurrentBag")]
        public int BatchShape_Parallel()
        {
            var results = new ConcurrentBag<Guid>();
            Parallel.For(0, Sends, _ => results.Add(_storage.SendMessage(_preparedMessage, _preparedData)));
            return results.Count;
        }

        /// <summary>The same work as an ordered loop into an array.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "batch shape: ordered loop into an array")]
        public int BatchShape_Loop()
        {
            var results = new Guid[Sends];
            for (var i = 0; i < Sends; i++)
                results[i] = _storage.SendMessage(_preparedMessage, _preparedData);
            return results.Length;
        }

        /// <summary>
        /// What the producer adds over <see cref="SendMessages_Decorated"/>, split three ways.
        /// </summary>
        /// <summary>
        /// What <c>Send(T)</c> builds per message when the caller supplies no data, as it was:
        /// four collections created up front whether or not anything is put in them.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "component: message data, collections eager (as it was)")]
        public void Component_MessageData_Eager()
        {
            var sink = _sink;
            for (var i = 0; i < Sends; i++)
                sink[i] = new EagerMessageData();
        }

        /// <summary>The same object as it is now, with the collections created on first use.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "component: message data, collections lazy (now)")]
        public void Component_MessageData_Lazy()
        {
            var sink = _sink;
            for (var i = 0; i < Sends; i++)
                sink[i] = new AdditionalMessageData();
        }

        /// <summary>
        /// <c>HeaderSetup</c> on its own: the correlation id it creates when the caller supplied
        /// none, and the two reads of <c>data.Headers</c> it makes.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "component: GenerateMessageHeaders.HeaderSetup")]
        public void Component_HeaderSetup()
        {
            for (var i = 0; i < Sends; i++)
            {
                _preparedData.CorrelationId = null;
                _generateHeaders.HeaderSetup(_preparedData);
            }
        }

        /// <summary>The message factory, which builds the header dictionary.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "component: IMessageFactory.Create")]
        public void Component_MessageFactory()
        {
            for (var i = 0; i < Sends; i++)
                _messageFactory.Create(new Event { Body = _payload }, null);
        }

        /// <summary>The three standard headers stamped on every outgoing message.</summary>
        [Benchmark(OperationsPerInvoke = Sends, Description = "component: AddStandardMessageHeaders.AddHeaders")]
        public void Component_AddStandardHeaders()
        {
            for (var i = 0; i < Sends; i++)
                _addStandardHeaders.AddHeaders(_preparedMessage, _preparedData);
        }

        public sealed class Event
        {
            public string Body { get; set; }
        }

        /// <summary>
        /// The four collections <see cref="AdditionalMessageData"/> used to build in its
        /// constructor, kept here so the pair above can be measured in one run rather than
        /// against a number recorded from an earlier build.
        /// </summary>
        private sealed class EagerMessageData
        {
            public EagerMessageData()
            {
                AdditionalMetaData = new List<IAdditionalMetaData>();
                TraceTags = new Dictionary<string, string>();
                Headers = new Dictionary<string, object>();
                Settings = new ConcurrentDictionary<string, object>();
            }

            public List<IAdditionalMetaData> AdditionalMetaData { get; }
            public Dictionary<string, string> TraceTags { get; }
            public Dictionary<string, object> Headers { get; }
            public ConcurrentDictionary<string, object> Settings { get; }
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Deliberate, and confined to a benchmark that is never shipped. Reading the live send chain is the " +
                            "point: the ladder measures the configured instances with their decorators, rather than a " +
                            "hand-built approximation that could silently drift from the real send path. Every lookup throws " +
                            "with an explanatory message if the layout changes.")]
        private static object Field(object target, string name)
        {
            var value = target?.GetType().GetField(name, Flags)?.GetValue(target);
            return value ?? throw new InvalidOperationException(
                $"Could not read '{name}' off {target?.GetType().Name ?? "null"}. The decorator layout or field " +
                "names changed; update MemoryPathBenchmarks.");
        }

        /// <summary>
        /// Walks decorators until it reaches the instance that actually does the work — identified
        /// by a field only the innermost implementation has.
        /// </summary>
        private static object Innermost(object node, string fieldOnlyTheInnermostHas)
        {
            for (var i = 0; i < 12; i++)
            {
                if (node.GetType().GetField(fieldOnlyTheInnermostHas, Flags) != null) return node;
                node = node.GetType().GetField("_handler", Flags)?.GetValue(node)
                       ?? node.GetType().GetField("_decorated", Flags)?.GetValue(node)
                       ?? node.GetType().GetField("_inner", Flags)?.GetValue(node)
                       ?? throw new InvalidOperationException(
                           "Ran out of decorators before finding one with a " +
                           $"'{fieldOnlyTheInnermostHas}' field. Update MemoryPathBenchmarks.");
            }
            throw new InvalidOperationException(
                $"Unwrapped 12 decorators without finding '{fieldOnlyTheInnermostHas}'. Update MemoryPathBenchmarks.");
        }
    }
}
