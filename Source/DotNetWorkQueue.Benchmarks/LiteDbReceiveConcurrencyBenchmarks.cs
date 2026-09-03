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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// The de-queue counterpart to <see cref="LiteDbConcurrencyBenchmarks"/>: whether concurrent
    /// consumers scale, which nothing has measured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ReceiveMessageQueryHandler</c> holds a process-wide <c>static</c> lock around every
    /// de-queue. That is deliberate and load-bearing - <c>BeginTrans</c> does not block in direct
    /// or memory mode, so without it two consumers can claim the same record - but its cost has
    /// never been measured. The send path had a lock of exactly this shape and it was the largest
    /// finding of the whole LiteDb pass: four producer threads ran <em>slower</em> than one, and
    /// two queues with separate database files performed no better than one.
    /// </para>
    /// <para>
    /// The rung that answers the question is <see cref="FourThreadsTwoQueues"/>. Two unrelated
    /// queues, each its own file, share nothing in the storage engine. If that row matches the
    /// single-queue row, the lock they are waiting on is process-wide rather than per queue, and
    /// the same defect shape is present on the receive side.
    /// </para>
    /// <para>
    /// The raw rung is the control, and it is what keeps the attribution honest: LiteDB takes an
    /// exclusive engine lock for a write transaction, so if the raw shape does not scale either
    /// then the ceiling belongs to the storage engine and not to this library. Getting that
    /// backwards is exactly the mistake #240 had to correct in place.
    /// </para>
    /// <para>
    /// Each iteration builds its queues from scratch. A de-queue marks a message processed rather
    /// than deleting it, and the walk introduced in #241 steps over ineligible rows in key order -
    /// so messages left behind by a previous iteration would make each later iteration slower and
    /// quietly turn a concurrency measurement into a queue-depth one.
    /// </para>
    /// </remarks>
    /// <para>
    /// Deliberately <b>not</b> a <c>[MemoryDiagnoser]</c> suite. Each iteration is a single
    /// invocation with the fixtures built in <c>[IterationSetup]</c>, and the diagnoser counts
    /// allocations across the whole iteration - so it would report the two queues, six containers,
    /// sixteen receive chains and four hundred seeded messages as if they were de-queue cost. It
    /// read 98 MB per two hundred de-queues that way. Allocation on this path belongs to
    /// <see cref="LiteDbReceiveBenchmarks"/>, which measures it without a fixture in the frame.
    /// </para>
    [InvocationCount(1)]
    public class LiteDbReceiveConcurrencyBenchmarks
    {
        private const int TotalMessages = 200;
        private const int PayloadBytes = 256;
        private const int MaxThreads = 8;

        private string _dir;
        private string _payload;
        private List<LiteDbPathBenchmarks.Event> _seed;

        private Fixture _a, _b;

        private static readonly object RawClaimLock = new object();
        private int _rawClaims;
        private LiteDatabase _rawDatabase;
        private ILiteCollection<RawStatus> _rawMeta;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _payload = new string('x', PayloadBytes);
            _seed = new List<LiteDbPathBenchmarks.Event>(TotalMessages);
            for (var i = 0; i < TotalMessages; i++)
                _seed.Add(new LiteDbPathBenchmarks.Event { Body = _payload });

            _dir = Path.Combine(Path.GetTempPath(), "dnwq-litedb-recv-conc", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _a = Fixture.Create(_dir, "a", _seed);
            _b = Fixture.Create(_dir, "b", _seed);

            var rawPath = Path.Combine(_dir, "raw-" + Guid.NewGuid().ToString("N") + ".db");
            _rawDatabase = new LiteDatabase($"Filename={rawPath};Connection=direct;");
            _rawMeta = _rawDatabase.GetCollection<RawStatus>("qmeta");
            _rawMeta.EnsureIndex(x => x.Id);
            var rows = new List<RawStatus>(TotalMessages);
            for (var i = 0; i < TotalMessages; i++) rows.Add(new RawStatus { Status = 0 });
            _rawMeta.InsertBulk(rows);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _a?.Dispose();
            _b?.Dispose();
            _rawDatabase?.Dispose();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* a file still held open is not worth failing a run over */ }
            catch (UnauthorizedAccessException) { /* same */ }
        }

        /// <summary>The serial baseline every other rung is compared against.</summary>
        [Benchmark(Baseline = true, Description = "200 de-queues, 1 thread, one queue")]
        public void OneThread()
        {
            for (var i = 0; i < TotalMessages; i++) _a.Dequeue(0);
        }

        /// <summary>
        /// The same work over four threads. If it is no faster than one thread, something is
        /// serializing the consumers.
        /// </summary>
        [Benchmark(Description = "200 de-queues, 4 threads, one queue")]
        public void FourThreads() => Spread(4, t => _a.Dequeue(t));

        /// <summary>Eight threads, to show whether the ceiling moves at all.</summary>
        [Benchmark(Description = "200 de-queues, 8 threads, one queue")]
        public void EightThreads() => Spread(8, t => _a.Dequeue(t));

        /// <summary>
        /// Four threads split across <b>two unrelated queues</b>, each with its own database file.
        /// Nothing in the storage engine forces these to interfere, so if this matches the
        /// one-queue row the lock they share is process-wide rather than per queue.
        /// </summary>
        [Benchmark(Description = "200 de-queues, 4 threads, two separate queues")]
        public void FourThreadsTwoQueues()
            => Spread(4, t => (t % 2 == 0 ? _a : _b).Dequeue(t));

        /// <summary>
        /// The control: the same claim-a-row transaction straight into LiteDB on four threads,
        /// with no transport in the way. Shows what the storage engine alone allows.
        /// </summary>
        [Benchmark(Description = "200 raw LiteDB claims, 4 threads (control)")]
        public void FourThreadsRaw()
        {
            _rawClaims = 0;
            Spread(4, _ => RawClaim());
            VerifyRawClaims();
        }

        /// <summary>The same on one thread, so the control has its own baseline.</summary>
        [Benchmark(Description = "200 raw LiteDB claims, 1 thread (control)")]
        public void OneThreadRaw()
        {
            _rawClaims = 0;
            for (var i = 0; i < TotalMessages; i++) RawClaim();
            VerifyRawClaims();
        }

        /// <summary>Claims the next unclaimed row, the shape the de-queue writes.</summary>
        private void RawClaim()
        {
            //The lock is not incidental - it is the finding. Without it this races: BeginTrans does
            //not block in direct mode, so concurrent claim transactions interleave and take the
            //same row. Measured, four unsynchronized threads made 200 claims that left only 63 of
            //200 rows claimed, and ran "faster" precisely because most of the work was wrong. So
            //the floor for a *correct* claim is a serialized one, which is what the transport does.
            lock (RawClaimLock)
            {
                _rawDatabase.BeginTrans();
                try
                {
                    var row = _rawMeta.Query().Where(x => x.Status == 0).Limit(1).FirstOrDefault();
                    if (row != null)
                    {
                        row.Status = 1;
                        row.HeartBeat = DateTime.UtcNow;
                        _rawMeta.Update(row);
                        Interlocked.Increment(ref _rawClaims);
                    }
                }
                finally
                {
                    _rawDatabase.Commit();
                }
            }
        }

        /// <summary>
        /// A claim that found a row still has to have been the only one to find it. BeginTrans does
        /// not block in direct mode, so without this the control could race - two threads claiming
        /// the same row do less work than two threads claiming two rows, and the rung would look
        /// fast because it was wrong. That is the whole reason the transport holds a lock here.
        /// </summary>
        private void VerifyRawClaims()
        {
            var claimed = _rawMeta.Count(x => x.Status == 1);
            if (_rawClaims != TotalMessages || claimed != TotalMessages)
                throw new InvalidOperationException(
                    $"raw control did not claim every row exactly once: {_rawClaims} claims left {claimed} rows claimed " +
                    $"of {TotalMessages}. The comparison is invalid.");
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

        /// <summary>Stands in for the transport's meta collection, for the raw control.</summary>
        public sealed class RawStatus
        {
            public int Id { get; set; }
            public int Status { get; set; }
            public DateTime? HeartBeat { get; set; }
        }

        /// <summary>
        /// One queue, seeded, with a receive chain per thread - each worker in a running consumer
        /// has its own, so the benchmark gives each thread its own rather than sharing one.
        /// </summary>
        private sealed class Fixture : IDisposable
        {
            private QueueCreationContainer<LiteDbMessageQueueInit> _creation;
            private QueueContainer<LiteDbMessageQueueInit> _producerContainer;
            private IProducerQueue<LiteDbPathBenchmarks.Event> _producer;
            private QueueContainer<LiteDbMessageQueueInit> _consumerContainer;
            private IConsumerQueue _consumer;

            private IMessageContextFactory _contextFactory;
            private IReceiveMessages[] _receivers;

            public static Fixture Create(string dir, string name, List<LiteDbPathBenchmarks.Event> seed)
            {
                var file = Path.Combine(dir, $"{name}-{Guid.NewGuid():N}.db");
                var connectionString = $"Filename={file};Connection=direct;";
                var queueName = $"benchRecvConc{name}{Guid.NewGuid():N}";

                var fixture = new Fixture();
                (fixture._creation, fixture._producerContainer, fixture._producer) =
                    LiteDbPathBenchmarks.CreateQueue(queueName, connectionString);

                //the batch path, so seeding does not dominate the setup
                foreach (var result in fixture._producer.Send(seed))
                {
                    if (result.HasError)
                        throw result.SendingException ?? new InvalidOperationException("seed send failed");
                }

                //never started - starting it would put worker threads on the queue this benchmark
                //is draining. It exists so the container builds the receive chain as a running
                //consumer would, and the rungs then drive it directly.
                var connection = new QueueConnection(queueName, connectionString);
                fixture._consumerContainer = new QueueContainer<LiteDbMessageQueueInit>();
                fixture._consumer = fixture._consumerContainer.CreateConsumer(connection);

                var container = ConsumerInternals.ContainerOf(fixture._consumerContainer);
                fixture._contextFactory = container.GetInstance<IMessageContextFactory>();

                var factory = container.GetInstance<IReceiveMessagesFactory>();
                fixture._receivers = new IReceiveMessages[MaxThreads];
                for (var i = 0; i < MaxThreads; i++) fixture._receivers[i] = factory.Create();

                return fixture;
            }

            public void Dequeue(int thread)
            {
                using var context = _contextFactory.Create();
                var message = _receivers[thread % MaxThreads].ReceiveMessage(context);

                //exactly as many messages are seeded as are taken, and the de-queue is serialized,
                //so a null here means the queue was not seeded as intended - which would quietly
                //turn every rung into a measurement of empty polls
                if (message == null)
                    throw new InvalidOperationException("de-queue found nothing; the fixture is not seeded as expected");
            }

            public void Dispose()
            {
                _consumer?.Dispose();
                _consumerContainer?.Dispose();
                _producer?.Dispose();
                _producerContainer?.Dispose();
                _creation?.Dispose();
            }
        }
    }
}
