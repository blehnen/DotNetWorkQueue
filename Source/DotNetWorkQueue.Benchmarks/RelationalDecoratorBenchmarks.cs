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
using System.IO;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// What the retry decorator costs per command, separated from the database call it wraps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both #231 and #232 ask what the decorator stack costs, noting that a win there is shared by
    /// all three relational transports rather than being transport-local. It cannot be answered
    /// from the send ladders: the decorator sits around a call that takes milliseconds, so its own
    /// cost disappears into the noise of the round trip.
    /// </para>
    /// <para>
    /// So the inner handler here does nothing. What is left between the two rungs is the decorator:
    /// a registry lookup, Polly's <c>Execute</c>, and the closure that
    /// <c>pipeline.Execute(_ =&gt; _decorated.Handle(command))</c> allocates because it captures
    /// both the command and the handler.
    /// </para>
    /// <para>
    /// SQLite is used because it needs no server and carries the same decorator. The three
    /// transports each have their own copy of this class; they differ only in an
    /// <c>IRetrySkippable</c> short-circuit that SQLite cannot reach, since it never builds the
    /// shared command that implements it.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class RelationalDecoratorBenchmarks
    {
        private string _dir;
        private QueueCreationContainer<SqLiteMessageQueueInit> _creation;
        private QueueContainer<SqLiteMessageQueueInit> _container;
        private IProducerQueue<SendPathBenchmarks.Event> _producer;

        private StubHandler _inner;
        private ICommandHandlerWithOutput<StubCommand, int> _decorated;
        private StubCommand _command;

        [GlobalSetup]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-decorators", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            var file = Path.Combine(_dir, "decorators.db3");
            var connection = new QueueConnection("benchDecorators",
                $"Data Source={file};Version=3;Synchronous=NORMAL;Pooling=True;");

            _creation = new QueueCreationContainer<SqLiteMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<SqLiteMessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status}");
            }

            //a real queue only so the container hands back the real policies, with whatever retry
            //pipeline the transport registers - the point is to measure the decorator as configured
            _container = new QueueContainer<SqLiteMessageQueueInit>();
            _producer = _container.CreateProducer<SendPathBenchmarks.Event>(connection);
            var container = ConsumerInternals.ContainerOf(_container);

            _inner = new StubHandler();
            _decorated = new RetryCommandHandlerOutputDecorator<StubCommand, int>(
                _inner, container.GetInstance<IPolicies>());
            _command = new StubCommand();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _producer?.Dispose();
            _container?.Dispose();
            _creation?.Dispose();
            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* a held file is not worth failing a run over */ }
            catch (UnauthorizedAccessException) { /* same */ }
        }

        /// <summary>The floor: the handler on its own.</summary>
        [Benchmark(Baseline = true, Description = "handler alone (no decorator)")]
        public int Handler_Direct() => _inner.Handle(_command);

        /// <summary>The same handler through the retry decorator, as the transports register it.</summary>
        [Benchmark(Description = "handler through the retry decorator")]
        public int Handler_Decorated() => _decorated.Handle(_command);

        /// <summary>A command with nothing in it; the decorator does not look inside.</summary>
        public sealed class StubCommand;

        /// <summary>An inner handler that does nothing, so only the decorator is measured.</summary>
        public sealed class StubHandler : ICommandHandlerWithOutput<StubCommand, int>
        {
            public int Handle(StubCommand command) => 1;
        }
    }
}
