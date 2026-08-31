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
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Measures what a single-threaded ladder cannot see: whether concurrent producers scale.
    /// </summary>
    /// <remarks>
    /// <c>SendMessageCommandHandler</c> takes a <b>static</b> lock around the whole send, so every
    /// send in the process serializes - including sends to unrelated queues. Each rung here sends a
    /// fixed number of messages and is reported per batch, so the rows are directly comparable:
    /// if more threads do not make the batch faster, the lock is the ceiling.
    /// <para>
    /// The raw rung is the control. LiteDB does its own locking internally, so it shows what the
    /// storage engine alone allows before the transport's lock is added.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class LiteDbConcurrencyBenchmarks
    {
        private const int TotalMessages = 200;
        private const int PayloadBytes = 256;

        private string _dir;
        private string _payload;

        private QueueCreationContainer<LiteDbMessageQueueInit> _creationA, _creationB;
        private QueueContainer<LiteDbMessageQueueInit> _containerA, _containerB;
        private IProducerQueue<LiteDbPathBenchmarks.Event> _producerA, _producerB;

        private LiteDatabase _rawDatabase;
        private ILiteCollection<LiteDbPathBenchmarks.RawQueue> _rawQueue;
        private ILiteCollection<LiteDbPathBenchmarks.RawMeta> _rawMeta;

        [GlobalSetup]
        public void Setup()
        {
            _payload = new string('x', PayloadBytes);
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-litedb-conc", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);

            (_creationA, _containerA, _producerA) = Create("benchConcA", Path.Combine(_dir, "a.db"));
            (_creationB, _containerB, _producerB) = Create("benchConcB", Path.Combine(_dir, "b.db"));

            _rawDatabase = new LiteDatabase($"Filename={Path.Combine(_dir, "raw.db")};Connection=direct;");
            _rawQueue = _rawDatabase.GetCollection<LiteDbPathBenchmarks.RawQueue>("q");
            _rawMeta = _rawDatabase.GetCollection<LiteDbPathBenchmarks.RawMeta>("qmeta");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _producerA?.Dispose(); _containerA?.Dispose(); _creationA?.Dispose();
            _producerB?.Dispose(); _containerB?.Dispose(); _creationB?.Dispose();
            _rawDatabase?.Dispose();
            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* leave it behind */ }
            catch (UnauthorizedAccessException) { /* leave it behind */ }
        }

        /// <summary>The serial baseline every other rung is compared against.</summary>
        [Benchmark(Baseline = true, Description = "200 sends, 1 thread, one queue")]
        public void OneThread()
        {
            for (var i = 0; i < TotalMessages; i++) Send(_producerA);
        }

        /// <summary>
        /// The same work spread over four threads. If this is no faster than one thread, the
        /// transport is serializing them.
        /// </summary>
        [Benchmark(Description = "200 sends, 4 threads, one queue")]
        public void FourThreads() => Spread(4, _ => Send(_producerA));

        /// <summary>Eight threads, to show whether the ceiling moves at all.</summary>
        [Benchmark(Description = "200 sends, 8 threads, one queue")]
        public void EightThreads() => Spread(8, _ => Send(_producerA));

        /// <summary>
        /// Four threads split across <b>two unrelated queues</b>, each with its own database file.
        /// Nothing about the storage engine forces these to interfere. If this matches the
        /// one-queue row, the lock they share is process-wide rather than per queue.
        /// </summary>
        [Benchmark(Description = "200 sends, 4 threads, two separate queues")]
        public void FourThreadsTwoQueues()
            => Spread(4, i => Send(i % 2 == 0 ? _producerA : _producerB));

        /// <summary>
        /// The control: the same number of inserts straight into LiteDB on four threads, with no
        /// transport in the way. Shows what the storage engine alone allows.
        /// </summary>
        [Benchmark(Description = "200 raw LiteDB inserts, 4 threads (control)")]
        public void FourThreadsRaw()
            => Spread(4, _ => _rawQueue.Insert(
                new LiteDbPathBenchmarks.RawQueue { Body = new byte[PayloadBytes], Headers = new byte[64] }));

        /// <summary>
        /// The second control, and the one that matters for attribution: the same shape the
        /// transport writes - a transaction around two collection inserts - straight into LiteDB on
        /// four threads. A write transaction takes an exclusive engine lock, so if this does not
        /// scale either then the single-queue ceiling belongs to the storage engine rather than to
        /// this library.
        /// </summary>
        [Benchmark(Description = "200 raw LiteDB DNWQ-shape writes, 4 threads (control)")]
        public void FourThreadsRawTransaction()
            => Spread(4, _ =>
            {
                _rawDatabase.BeginTrans();
                var id = _rawQueue.Insert(new LiteDbPathBenchmarks.RawQueue
                    { Body = new byte[PayloadBytes], Headers = new byte[64] }).AsInt32;
                _rawMeta.Insert(new LiteDbPathBenchmarks.RawMeta
                {
                    QueueId = id,
                    CorrelationId = Guid.NewGuid(),
                    QueuedDateTime = DateTimeOffset.UtcNow
                });
                _rawDatabase.Commit();
            });

        /// <summary>The same, on one thread, so the control has its own baseline.</summary>
        [Benchmark(Description = "200 raw LiteDB DNWQ-shape writes, 1 thread (control)")]
        public void OneThreadRawTransaction()
        {
            for (var i = 0; i < TotalMessages; i++)
            {
                _rawDatabase.BeginTrans();
                var id = _rawQueue.Insert(new LiteDbPathBenchmarks.RawQueue
                    { Body = new byte[PayloadBytes], Headers = new byte[64] }).AsInt32;
                _rawMeta.Insert(new LiteDbPathBenchmarks.RawMeta
                {
                    QueueId = id,
                    CorrelationId = Guid.NewGuid(),
                    QueuedDateTime = DateTimeOffset.UtcNow
                });
                _rawDatabase.Commit();
            }
        }

        private void Send(IProducerQueue<LiteDbPathBenchmarks.Event> producer)
        {
            var result = producer.Send(new LiteDbPathBenchmarks.Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>Splits <see cref="TotalMessages"/> evenly over <paramref name="threads"/> tasks.</summary>
        private static void Spread(int threads, Action<int> work)
        {
            var per = TotalMessages / threads;
            var tasks = new Task[threads];
            for (var t = 0; t < threads; t++)
            {
                var index = t;
                tasks[t] = Task.Run(() =>
                {
                    for (var i = 0; i < per; i++) work(index);
                });
            }
            Task.WaitAll(tasks);
        }

        private static (QueueCreationContainer<LiteDbMessageQueueInit>, QueueContainer<LiteDbMessageQueueInit>,
            IProducerQueue<LiteDbPathBenchmarks.Event>) Create(string name, string path)
        {
            var connection = new QueueConnection(name, $"Filename={path};Connection=direct;");
            var creation = new QueueCreationContainer<LiteDbMessageQueueInit>();
            using (var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }
            var container = new QueueContainer<LiteDbMessageQueueInit>();
            return (creation, container, container.CreateProducer<LiteDbPathBenchmarks.Event>(connection));
        }
    }
}
