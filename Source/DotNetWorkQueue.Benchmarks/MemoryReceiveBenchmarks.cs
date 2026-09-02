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
using System.Threading;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory;
using MemoryBasic = DotNetWorkQueue.Transport.Memory.Basic;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// The consume-side counterpart to <see cref="MemoryPathBenchmarks"/>: what it costs the core
    /// library to hand a caller one message, with no transport work in the number.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read it as a ladder, the difference between adjacent rungs being what the upper one adds:
    /// </para>
    /// <list type="number">
    /// <item><description><see cref="RawStore"/> — the floor: taking an id off the collection the
    /// transport queues into and looking the item up.</description></item>
    /// <item><description><see cref="DataStorage_GetNextMessage"/> — the transport's storage
    /// layer.</description></item>
    /// <item><description><see cref="ReceiveMessages_Undecorated"/> — the transport's
    /// <see cref="IReceiveMessages"/>, with a message context created per message as the consume
    /// loop does.</description></item>
    /// <item><description><see cref="ReceiveMessages_Decorated"/> — *minus the row above* = the
    /// four decorators the container wraps it in: policy, trace, history and metrics.
    /// </description></item>
    /// </list>
    /// <para>
    /// Each rung consumes exactly the messages seeded for the iteration and no more. That is not
    /// tidiness: a receive against an empty queue blocks for five seconds, so over-consuming would
    /// not report a slow benchmark, it would report a hung one.
    /// </para>
    /// <para>
    /// The consume loop's remaining half — the user's handler, the heartbeat worker and the commit
    /// — is not here. Reaching <c>ProcessMessage</c> means registering a handler through
    /// <c>Start</c>, which puts worker threads on the same queue this benchmark is draining.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(1)]
    public class MemoryReceiveBenchmarks
    {
        /// <summary>Messages seeded per iteration, and consumed per invocation.</summary>
        private const int Messages = 5000;
        private const int PayloadBytes = 256;

        private string _payload;
        private QueueConnection _queueConnection;

        private QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit> _creation;
        private QueueContainer<MemoryBasic.MemoryMessageQueueInit> _producerContainer;
        private QueueContainer<MemoryBasic.MemoryMessageQueueInit> _consumerContainer;
        private IProducerQueue<Event> _producer;
        private IConsumerQueue _consumer;

        private IDataStorage _storage;
        private IReceiveMessages _receiveDecorated;
        private IReceiveMessages _receiveUndecorated;
        private IMessageContextFactory _contextFactory;
        private IQueueCancelWork _cancelWork;

        private ConcurrentDictionary<Guid, object> _rawData;
        private BlockingCollection<Guid> _rawQueue;

        private CancellationTokenSource _sharedLinked;

        private IMessageFactory _messageFactory;
        private IReceivedMessageFactory _receivedMessageFactory;
        private IWorkerNotificationFactory _workerNotificationFactory;
        private Dictionary<string, object> _headers;
        private IMessage _message;

        //the two ways of getting a context out of the container: the lookup the factory does per
        //message today, and the compiled producer that lookup ends at
        private SimpleInjector.Container _simpleInjector;
        private SimpleInjector.InstanceProducer _contextProducer;

        //for the event-wiring pair
        private EventHandler _cachedCommit;
        private EventHandler _cachedRollback;
        private EventHandler _cachedCleanup;

        //the six dependencies a WorkerNotification takes, pulled out once so the object can be
        //built directly and compared with resolving it
        private object[] _workerNotificationArgs;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _payload = new string('x', PayloadBytes);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _queueConnection = new QueueConnection("benchMemoryReceive" + Guid.NewGuid().ToString("N"), "memory");

            _creation = new QueueCreationContainer<MemoryBasic.MemoryMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<MemoryBasic.MessageQueueCreation>(_queueConnection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"memory CreateQueue failed: {result.Status}");
            }

            _producerContainer = new QueueContainer<MemoryBasic.MemoryMessageQueueInit>();
            _producer = _producerContainer.CreateProducer<Event>(_queueConnection);
            for (var i = 0; i < Messages; i++)
            {
                var result = _producer.Send(new Event { Body = _payload });
                if (result.HasError)
                    throw result.SendingException ?? new InvalidOperationException("seed send failed");
            }

            //the consumer is never started - starting it would put worker threads on the same
            //queue this benchmark is draining. It exists so the container builds the receive chain
            //exactly as a running consumer would, and the pieces are then driven directly.
            _consumerContainer = new QueueContainer<MemoryBasic.MemoryMessageQueueInit>();
            _consumer = _consumerContainer.CreateConsumer(_queueConnection);

            var container = ConsumerContainer(_consumerContainer);
            _storage = container.GetInstance<IDataStorage>();
            _contextFactory = container.GetInstance<IMessageContextFactory>();
            _cancelWork = container.GetInstance<IQueueCancelWork>();
            _receiveDecorated = container.GetInstance<IReceiveMessagesFactory>().Create();
            _receiveUndecorated = (IReceiveMessages)Innermost(_receiveDecorated, "_receiveMessages");

            _messageFactory = container.GetInstance<IMessageFactory>();
            _receivedMessageFactory = container.GetInstance<IReceivedMessageFactory>();
            _workerNotificationFactory = container.GetInstance<IWorkerNotificationFactory>();

            _simpleInjector = SimpleInjectorOf(container);
            _contextProducer = _simpleInjector.GetRegistration(typeof(IMessageContext), true);

            //a header set of the shape a real message carries out of the store
            _headers = new Dictionary<string, object>
            {
                { "Queue-FirstPossibleDeliveryDate", new ValueTypeWrapper<DateTime>(DateTime.UtcNow) },
                { "Queue-SerializerId", "Newtonsoft" },
                { "Queue-MessageBodyType", typeof(Event).FullName + ", " + typeof(Event).Assembly.GetName().Name },
                { "Queue-MessageInterceptorGraph", "none" }
            };
            _message = _messageFactory.Create(new Event { Body = _payload }, _headers);

            _workerNotificationArgs = new object[]
            {
                container.GetInstance<IHeaders>(),
                _cancelWork,
                container.GetInstance<DotNetWorkQueue.Configuration.TransportConfigurationReceive>(),
                container.GetInstance<Microsoft.Extensions.Logging.ILogger>(),
                container.GetInstance<IMetrics>(),
                container.GetInstance<System.Diagnostics.ActivitySource>()
            };

            _cachedCommit = OnEvent;
            _cachedRollback = OnEvent;
            _cachedCleanup = OnEvent;

            _rawData = new ConcurrentDictionary<Guid, object>();
            _rawQueue = new BlockingCollection<Guid>();
            for (var i = 0; i < Messages; i++)
            {
                var id = Guid.NewGuid();
                _rawData.TryAdd(id, _payload);
                _rawQueue.Add(id);
            }

            _sharedLinked?.Dispose();
            _sharedLinked = CancellationTokenSource.CreateLinkedTokenSource(
                _cancelWork.CancelWorkToken, _cancelWork.StopWorkToken);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _sharedLinked?.Dispose();
            (_storage as IDataStorage)?.Clear();
            _consumer?.Dispose();
            _consumerContainer?.Dispose();
            _producer?.Dispose();
            _producerContainer?.Dispose();
            _creation?.Dispose();
            _rawQueue?.Dispose();

            //an iteration leaves thousands of live objects behind; collecting them here rather
            //than during the next measurement keeps a GC out of the timings
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>The floor: taking an id off the queue and looking the item up.</summary>
        [Benchmark(Baseline = true, OperationsPerInvoke = Messages, Description = "raw store: take + lookup")]
        public void RawStore()
        {
            for (var i = 0; i < Messages; i++)
            {
                if (!_rawQueue.TryTake(out var id)) throw new InvalidOperationException("ran dry");
                _rawData.TryRemove(id, out _);
            }
        }

        /// <summary>The transport's storage layer.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "DataStorage.GetNextMessage")]
        public void DataStorage_GetNextMessage()
        {
            for (var i = 0; i < Messages; i++)
            {
                if (_storage.GetNextMessage(null, TimeSpan.Zero) == null)
                    throw new InvalidOperationException("ran dry");
            }
        }

        /// <summary>The transport's <see cref="IReceiveMessages"/>, plus the per-message context.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "IReceiveMessages, undecorated")]
        public void ReceiveMessages_Undecorated()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = _contextFactory.Create();
                if (_receiveUndecorated.ReceiveMessage(context) == null)
                    throw new InvalidOperationException("ran dry");
            }
        }

        /// <summary>The same through the policy, trace, history and metrics decorators.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "IReceiveMessages, decorated (policy/trace/history/metrics)")]
        public void ReceiveMessages_Decorated()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = _contextFactory.Create();
                if (_receiveDecorated.ReceiveMessage(context) == null)
                    throw new InvalidOperationException("ran dry");
            }
        }

        /// <summary>The message context the consume loop builds per message.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: IMessageContextFactory.Create")]
        public void Component_MessageContext()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = _contextFactory.Create();
                if (context == null) throw new InvalidOperationException("no context");
            }
        }

        /// <summary>
        /// What the storage layer does per message to combine the two cancellation tokens, as it
        /// was: a linked source built and thrown away for every message.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: linked token source, per message")]
        public void Component_LinkedTokenPerMessage()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                    _cancelWork.CancelWorkToken, _cancelWork.StopWorkToken);
                if (linked.IsCancellationRequested) throw new InvalidOperationException("cancelled");
            }
        }

        /// <summary>The same two tokens combined once and reused.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: linked token source, built once")]
        public void Component_LinkedTokenShared()
        {
            for (var i = 0; i < Messages; i++)
            {
                if (_sharedLinked.IsCancellationRequested) throw new InvalidOperationException("cancelled");
            }
        }

        /// <summary>
        /// The message object the store rebuilds per de-queue, which copies the header dictionary.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: IMessageFactory.Create (with headers)")]
        public void Component_MessageFactory()
        {
            for (var i = 0; i < Messages; i++)
                _messageFactory.Create(new Event { Body = _payload }, _headers);
        }

        /// <summary>The wrapper the store returns, with its id and correlation id.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: IReceivedMessageFactory.Create")]
        public void Component_ReceivedMessageFactory()
        {
            for (var i = 0; i < Messages; i++)
            {
                _receivedMessageFactory.Create(_message, new MemoryBasic.MessageQueueId(Guid.Empty),
                    new MemoryBasic.MessageCorrelationId(Guid.Empty));
            }
        }

        /// <summary>The notification object every context carries.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: IWorkerNotificationFactory.Create")]
        public void Component_WorkerNotification()
        {
            for (var i = 0; i < Messages; i++)
                _workerNotificationFactory.Create();
        }

        /// <summary>
        /// The same object the factory above produces, constructed directly. Against that row,
        /// this is what the container costs as opposed to the object.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: WorkerNotification, constructed directly")]
        public void Component_WorkerNotificationDirect()
        {
            var a = _workerNotificationArgs;
            for (var i = 0; i < Messages; i++)
            {
                _ = new DotNetWorkQueue.Queue.WorkerNotification(
                    (IHeaders)a[0], (IQueueCancelWork)a[1],
                    (DotNetWorkQueue.Configuration.TransportConfigurationReceive)a[2],
                    (Microsoft.Extensions.Logging.ILogger)a[3], (IMetrics)a[4],
                    (System.Diagnostics.ActivitySource)a[5]);
            }
        }

        /// <summary>
        /// How the context factory gets a context today: a resolve from the container per message.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: container resolve of IMessageContext")]
        public void Component_ContainerResolve()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = (IMessageContext)_simpleInjector.GetInstance(typeof(IMessageContext));
                if (context == null) throw new InvalidOperationException("no context");
            }
        }

        /// <summary>
        /// The same graph built through the producer the resolve above ends at, looked up once.
        /// Against the row above, this is what the per-message type lookup costs.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: cached producer for IMessageContext")]
        public void Component_CachedProducer()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = (IMessageContext)_contextProducer.GetInstance();
                if (context == null) throw new InvalidOperationException("no context");
            }
        }

        /// <summary>
        /// The commit/rollback/cleanup wiring the receive does per message, subscribing method
        /// groups - each of which builds a delegate.
        /// </summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: event wiring, method groups")]
        public void Component_EventWiringMethodGroups()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = _contextFactory.Create();
                context.Commit += OnEvent;
                context.Rollback += OnEvent;
                context.Cleanup += OnEvent;
                context.Commit -= OnEvent;
                context.Rollback -= OnEvent;
                context.Cleanup -= OnEvent;
            }
        }

        /// <summary>The same wiring from delegates built once.</summary>
        [Benchmark(OperationsPerInvoke = Messages, Description = "component: event wiring, cached delegates")]
        public void Component_EventWiringCached()
        {
            for (var i = 0; i < Messages; i++)
            {
                using var context = _contextFactory.Create();
                context.Commit += _cachedCommit;
                context.Rollback += _cachedRollback;
                context.Cleanup += _cachedCleanup;
                context.Commit -= _cachedCommit;
                context.Rollback -= _cachedRollback;
                context.Cleanup -= _cachedCleanup;
            }
        }

        private void OnEvent(object sender, EventArgs e)
        {
        }

        public sealed class Event
        {
            public string Body { get; set; }
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// The consumer's own container, so the rungs resolve exactly the instances a running
        /// consumer would use rather than a hand-built approximation.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Deliberate, and confined to a benchmark that is never shipped. Reading the consumer's container is " +
                            "the point: the ladder measures the configured instances with their decorators, rather than a " +
                            "hand-built approximation that could silently drift from the real receive path.")]
        private static IContainer ConsumerContainer(object queueContainer)
        {
            var field = typeof(BaseContainer).GetField("Containers", Flags)
                        ?? throw new InvalidOperationException(
                            "BaseContainer no longer has a 'Containers' field; update MemoryReceiveBenchmarks.");

            var bag = (ConcurrentBag<IDisposable>)field.GetValue(queueContainer);
            foreach (var item in bag)
            {
                if (item is IContainer container) return container;
            }

            throw new InvalidOperationException(
                "No IContainer found on the queue container; update MemoryReceiveBenchmarks.");
        }

        /// <summary>
        /// The SimpleInjector container behind the wrapper, so the resolve the context factory
        /// makes per message can be compared with the producer it ends at.
        /// </summary>
        private static SimpleInjector.Container SimpleInjectorOf(IContainer container)
        {
            var field = container.GetType().GetField("_container", Flags)
                        ?? throw new InvalidOperationException(
                            "ContainerWrapper no longer has a '_container' field; update MemoryReceiveBenchmarks.");
            return (SimpleInjector.Container)field.GetValue(container);
        }

        /// <summary>Walks decorators to the instance that does the work.</summary>
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
                           $"'{fieldOnlyTheInnermostHas}' field. Update MemoryReceiveBenchmarks.");
            }
            throw new InvalidOperationException(
                $"Unwrapped 12 decorators without finding '{fieldOnlyTheInnermostHas}'. Update MemoryReceiveBenchmarks.");
        }
    }
}
