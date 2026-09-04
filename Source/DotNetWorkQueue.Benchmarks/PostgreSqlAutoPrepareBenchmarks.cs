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
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Whether Npgsql's automatic statement preparation is worth turning on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// #232 names this as the PostgreSQL-specific lever: it has no equivalent in the other
    /// providers, it is off by default (<c>Max Auto Prepare=0</c>), and the send path issues the
    /// same handful of statements over and over, which is the shape it targets. If it pays, it is a
    /// connection-string change rather than code - which is the cheapest kind of win there is, and
    /// exactly why it needs measuring rather than assuming.
    /// </para>
    /// <para>
    /// The rungs are paired: the same work against two queues that differ only in their connection
    /// string. Everything else - schema, options, payload - is identical.
    /// </para>
    /// <para>
    /// <b>Requires a PostgreSQL instance</b> via <c>DNWQ_POSTGRES_CONNECTION</c>.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class PostgreSqlAutoPrepareBenchmarks
    {
        private const int PayloadBytes = 256;

        /// <summary>
        /// Npgsql prepares a statement once it has been seen this many times on a connection.
        /// Lowered from the default of 5 so the steady state is reached quickly.
        /// </summary>
        private const string AutoPrepareSettings = "Max Auto Prepare=20;Auto Prepare Min Usages=2;";

        /// <summary>Npgsql's own default, stated rather than assumed. See <c>Setup</c>.</summary>
        private const string AutoPrepareDisabled = "Max Auto Prepare=0;";

        private string _payload;
        private Fixture _off;
        private Fixture _on;

        [GlobalSetup]
        public void Setup()
        {
            var baseConnection = Environment.GetEnvironmentVariable("DNWQ_POSTGRES_CONNECTION");
            if (string.IsNullOrWhiteSpace(baseConnection))
                throw new InvalidOperationException(
                    "Set DNWQ_POSTGRES_CONNECTION to a PostgreSQL connection string.");

            _payload = new string('x', PayloadBytes);

            //Both rungs state their setting explicitly rather than inheriting whatever the
            //environment's connection string happens to say. Passing it through unchanged would
            //mean a caller whose string already set Max Auto Prepare got a baseline that was not
            //off, and a comparison of a thing against itself - which would look like "no effect"
            //and be indistinguishable from a real result.
            var separator = baseConnection.TrimEnd().EndsWith(";", StringComparison.Ordinal) ? "" : ";";
            _off = Fixture.Create(baseConnection + separator + AutoPrepareDisabled, _payload);
            _on = Fixture.Create(baseConnection + separator + AutoPrepareSettings, _payload);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _off?.Dispose();
            _on?.Dispose();
            NpgsqlConnection.ClearAllPools();
        }

        /// <summary>A send with auto-prepare off, which is what ships today.</summary>
        [Benchmark(Baseline = true, Description = "send, auto-prepare off (as shipped)")]
        public void Send_AutoPrepareOff() => _off.Send(_payload);

        /// <summary>The same send with auto-prepare on, which is a connection-string change only.</summary>
        [Benchmark(Description = "send, auto-prepare on")]
        public void Send_AutoPrepareOn() => _on.Send(_payload);

        /// <summary>The batch path with auto-prepare off, reported per batch of 100.</summary>
        [Benchmark(Description = "batch of 100, auto-prepare off (as shipped)")]
        public void Batch_AutoPrepareOff() => _off.SendBatch();

        /// <summary>The batch path with auto-prepare on.</summary>
        [Benchmark(Description = "batch of 100, auto-prepare on")]
        public void Batch_AutoPrepareOn() => _on.SendBatch();

        /// <summary>The message body these rungs send.</summary>
        public sealed class Event
        {
            public string Body { get; set; }
        }

        /// <summary>One queue and its producer, built from a given connection string.</summary>
        private sealed class Fixture : IDisposable
        {
            private const int BatchSize = 100;

            private QueueCreationContainer<PostgreSqlMessageQueueInit> _creation;
            private QueueContainer<PostgreSqlMessageQueueInit> _container;
            private IProducerQueue<Event> _producer;
            private QueueConnection _queueConnection;
            private List<Event> _batch;
            private int _disposeCount;

            public static Fixture Create(string connectionString, string payload)
            {
                var fixture = new Fixture
                {
                    _queueConnection = new QueueConnection(
                        "benchpgprep" + Guid.NewGuid().ToString("N"), connectionString)
                };

                fixture._creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
                using (var creator = fixture._creation
                           .GetQueueCreation<PostgreSqlMessageQueueCreation>(fixture._queueConnection))
                {
                    var result = creator.CreateQueue();
                    if (!result.Success)
                        throw new InvalidOperationException(
                            $"CreateQueue failed: {result.Status} {result.ErrorMessage}");
                }

                fixture._container = new QueueContainer<PostgreSqlMessageQueueInit>();
                fixture._producer = fixture._container.CreateProducer<Event>(fixture._queueConnection);

                fixture._batch = new List<Event>(BatchSize);
                for (var i = 0; i < BatchSize; i++) fixture._batch.Add(new Event { Body = payload });

                return fixture;
            }

            public void Send(string payload)
            {
                var result = _producer.Send(new Event { Body = payload });
                if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
            }

            public void SendBatch()
            {
                foreach (var result in _producer.Send(_batch))
                {
                    if (result.HasError)
                        throw result.SendingException ?? new InvalidOperationException("batch send failed");
                }
            }

            public void Dispose()
            {
                if (Interlocked.Increment(ref _disposeCount) != 1) return;

                _producer?.Dispose();
                _container?.Dispose();
                try
                {
                    using var creator = _creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (PostgresException)
                {
                    //a queue left behind is not worth failing a run over
                }
                _creation?.Dispose();
            }
        }
    }
}
