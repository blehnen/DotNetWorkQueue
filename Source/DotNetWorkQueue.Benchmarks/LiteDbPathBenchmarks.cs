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
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using LiteDB;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a LiteDb send, the same way <see cref="SendPathBenchmarks"/> decomposes SQLite.
    /// Read adjacent rungs as a ladder: the difference between two is the cost of what the upper
    /// one adds.
    /// </summary>
    /// <remarks>
    /// LiteDb is the other embedded single-file transport, so the SQLite findings are the obvious
    /// hypotheses - but they are hypotheses, and this exists to test them rather than assume them.
    /// <para>
    /// One cost cannot appear here at all: <c>SendMessageCommandHandler</c> holds a <b>static</b>
    /// lock for the scheduled-job check, which serializes those sends across the whole process
    /// regardless of queue. A single-threaded benchmark cannot see a lock - see
    /// <see cref="LiteDbConcurrencyBenchmarks"/>, which is where that was found and measured.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class LiteDbPathBenchmarks
    {
        private const int PayloadBytes = 256;
        private const int BatchSize = 100;

        private string _dir;
        private string _directConnectionString;
        private string _rawPath;

        private LiteDatabase _heldDatabase;
        private ILiteCollection<RawQueue> _heldQueue;
        private ILiteCollection<RawMeta> _heldMeta;
        private List<RawQueue> _rawBatch;

        private QueueCreationContainer<LiteDbMessageQueueInit> _directCreation;
        private QueueContainer<LiteDbMessageQueueInit> _directContainer;
        private IProducerQueue<Event> _directProducer;

        private QueueCreationContainer<LiteDbMessageQueueInit> _sharedCreation;
        private QueueContainer<LiteDbMessageQueueInit> _sharedContainer;
        private IProducerQueue<Event> _sharedProducer;

        private string _payload;
        private List<Event> _batch;

        /// <summary>A message with a body large enough that serialization is not trivially free.</summary>
        public sealed class Event
        {
            public string Body { get; set; }
        }

        /// <summary>Stands in for the transport's queue collection.</summary>
        public sealed class RawQueue
        {
            public int Id { get; set; }
            public byte[] Body { get; set; }
            public byte[] Headers { get; set; }
        }

        /// <summary>Stands in for the transport's meta collection.</summary>
        public sealed class RawMeta
        {
            public int Id { get; set; }
            public int QueueId { get; set; }
            public Guid CorrelationId { get; set; }
            public DateTimeOffset QueuedDateTime { get; set; }
        }

        //Setup is per benchmark rather than shared: BenchmarkDotNet runs each in its own process,
        //so one [GlobalSetup] would make every process build every fixture - including two full
        //queues - for a rung that only opens a database.

        private void SetupCommon()
        {
            _payload = new string('x', PayloadBytes);
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-litedb-benchmarks", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [GlobalSetup(Targets = new[] { nameof(RawInsert_OneCollection), nameof(RawInsert_DnwqShape),
            nameof(RawBatch_OneTransaction), nameof(RawBatch_InsertBulk) })]
        public void SetupForRawHeld()
        {
            SetupCommon();
            _rawPath = Path.Combine(_dir, "raw.db");
            _heldDatabase = new LiteDatabase($"Filename={_rawPath};Connection=direct;");
            _heldQueue = _heldDatabase.GetCollection<RawQueue>("q");
            _heldMeta = _heldDatabase.GetCollection<RawMeta>("qmeta");
            _rawBatch = new List<RawQueue>(BatchSize);
            for (var i = 0; i < BatchSize; i++)
                _rawBatch.Add(new RawQueue { Body = new byte[PayloadBytes], Headers = new byte[64] });
        }

        [GlobalSetup(Targets = new[] { nameof(RawInsert_DatabasePerSend), nameof(Database_OpenClose) })]
        public void SetupForRawPerSend()
        {
            SetupCommon();
            _rawPath = Path.Combine(_dir, "persend.db");
            using var seed = new LiteDatabase($"Filename={_rawPath};Connection=direct;");
            seed.GetCollection<RawQueue>("q").Insert(new RawQueue { Body = [1], Headers = [1] });
        }

        [GlobalSetup(Targets = new[] { nameof(Direct_Send), nameof(Direct_SendBatch), nameof(DatabaseExists_Check) })]
        public void SetupForDirect()
        {
            SetupCommon();
            _directConnectionString = $"Filename={Path.Combine(_dir, "direct.db")};Connection=direct;";
            (_directCreation, _directContainer, _directProducer) = CreateQueue("benchDirect", _directConnectionString);

            _batch = new List<Event>(BatchSize);
            for (var i = 0; i < BatchSize; i++) _batch.Add(new Event { Body = _payload });
        }

        [GlobalSetup(Target = nameof(Shared_Send))]
        public void SetupForShared()
        {
            SetupCommon();
            var sharedConnectionString = $"Filename={Path.Combine(_dir, "shared.db")};Connection=shared;";
            (_sharedCreation, _sharedContainer, _sharedProducer) = CreateQueue("benchShared", sharedConnectionString);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _directProducer?.Dispose();
            _directContainer?.Dispose();
            _directCreation?.Dispose();
            _sharedProducer?.Dispose();
            _sharedContainer?.Dispose();
            _sharedCreation?.Dispose();
            _heldDatabase?.Dispose();

            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* a database still held open is not worth failing a run over */ }
            catch (UnauthorizedAccessException) { /* same */ }
        }

        #region the ladder

        /// <summary>The floor: one insert into one collection, on a database already open.</summary>
        [Benchmark(Baseline = true, Description = "raw LiteDB, 1 insert (held database)")]
        public void RawInsert_OneCollection()
        {
            _heldQueue.Insert(new RawQueue { Body = new byte[PayloadBytes], Headers = new byte[64] });
        }

        /// <summary>
        /// The shape the transport writes - the body collection then the meta collection, inside a
        /// transaction. Against the row above, this is what the extra collection and the
        /// transaction cost.
        /// </summary>
        [Benchmark(Description = "raw LiteDB, DNWQ shape (held database)")]
        public void RawInsert_DnwqShape()
        {
            _heldDatabase.BeginTrans();
            var id = _heldQueue.Insert(new RawQueue { Body = new byte[PayloadBytes], Headers = new byte[64] }).AsInt32;
            _heldMeta.Insert(new RawMeta
            {
                QueueId = id,
                CorrelationId = Guid.NewGuid(),
                QueuedDateTime = DateTimeOffset.UtcNow
            });
            _heldDatabase.Commit();
        }

        /// <summary>
        /// The ceiling for a real batch path: the whole batch in <b>one</b> transaction, inserting
        /// each message so its generated id comes back, on a database already open.
        /// </summary>
        /// <remarks>
        /// Against <see cref="RawInsert_DnwqShape"/> times 100 this shows what amortising the
        /// transaction is worth; against the end-to-end batch rung it shows how much of the current
        /// cost is the per-message loop rather than the storage.
        /// </remarks>
        [Benchmark(Description = "raw LiteDB, 100 writes in ONE transaction (batch ceiling)")]
        public void RawBatch_OneTransaction()
        {
            _heldDatabase.BeginTrans();
            //fresh rows each iteration: LiteDB stamps the generated Id onto the object it inserts,
            //so re-inserting the same instances would be a duplicate key on the second pass
            foreach (var source in _rawBatch)
            {
                var id = _heldQueue.Insert(new RawQueue { Body = source.Body, Headers = source.Headers }).AsInt32;
                _heldMeta.Insert(new RawMeta
                {
                    QueueId = id,
                    CorrelationId = Guid.NewGuid(),
                    QueuedDateTime = DateTimeOffset.UtcNow
                });
            }
            _heldDatabase.Commit();
        }

        /// <summary>
        /// The same batch through <c>InsertBulk</c>, which is the API that exists for this - but it
        /// returns a count rather than the generated ids, so it is only usable if the ids can be
        /// recovered another way. Measured to find out whether giving them up would even pay.
        /// </summary>
        [Benchmark(Description = "raw LiteDB, 100 writes via InsertBulk (no ids returned)")]
        public void RawBatch_InsertBulk()
        {
            _heldDatabase.BeginTrans();
            _heldQueue.InsertBulk(_rawBatch.Select(r => new RawQueue { Body = r.Body, Headers = r.Headers }));
            _heldDatabase.Commit();
        }

        /// <summary>
        /// The same shape with a LiteDatabase constructed per send, which is what the transport does
        /// on a shared connection. Against the row above, this is the connection lifecycle.
        /// </summary>
        [Benchmark(Description = "raw LiteDB, DNWQ shape + database per send")]
        public void RawInsert_DatabasePerSend()
        {
            using var db = new LiteDatabase($"Filename={_rawPath};Connection=direct;");
            db.BeginTrans();
            var id = db.GetCollection<RawQueue>("q")
                .Insert(new RawQueue { Body = new byte[PayloadBytes], Headers = new byte[64] }).AsInt32;
            db.GetCollection<RawMeta>("qmeta").Insert(new RawMeta
            {
                QueueId = id,
                CorrelationId = Guid.NewGuid(),
                QueuedDateTime = DateTimeOffset.UtcNow
            });
            db.Commit();
        }

        /// <summary>Constructing and disposing a LiteDatabase, doing no work at all.</summary>
        [Benchmark(Description = "LiteDatabase open + close, no work")]
        public void Database_OpenClose()
        {
            using var db = new LiteDatabase($"Filename={_rawPath};Connection=direct;");
        }

        /// <summary>
        /// The existence check the send path runs on every message. Replicated here rather than
        /// called, because the transport's implementation is internal - but it is a faithful copy:
        /// <c>LiteDbGetFileNameFromConnectionString.GetFileName</c> builds a
        /// <see cref="LiteDB.ConnectionString"/>, tests for <c>:memory:</c>, allocates a result, and
        /// then <c>DatabaseExists.Exists</c> calls <see cref="File.Exists"/>. Nothing is cached, so
        /// every send pays the parse and the stat.
        /// </summary>
        [Benchmark(Description = "existence check (parse + stat, once per send)")]
        public bool DatabaseExists_Check()
        {
            var connection = new LiteDB.ConnectionString(_directConnectionString);
            var inMemory = _directConnectionString.Contains(":memory:");
            if (inMemory) return true;
            return File.Exists(connection.Filename);
        }

        /// <summary>The whole send path on a direct connection, as a caller experiences it.</summary>
        [Benchmark(Description = "DotNetWorkQueue LiteDb send, direct (end to end)")]
        public void Direct_Send()
        {
            var result = _directProducer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>
        /// The same on a shared connection, where the connection manager builds a new LiteDatabase
        /// for every operation. Against the row above, this is what choosing shared costs.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue LiteDb send, shared (end to end)")]
        public void Shared_Send()
        {
            var result = _sharedProducer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>
        /// A batch send, reported per batch. LiteDb has no bulk path and falls back to a loop of
        /// single sends, so divide by the batch size and compare with the single-send rung to see
        /// whether batching currently buys anything at all.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue LiteDb batch send (100 messages)")]
        public void Direct_SendBatch()
        {
            var results = _directProducer.Send(_batch);
            if (results.HasErrors) throw new InvalidOperationException("batch send failed");
        }

        #endregion

        /// <summary>
        /// Creates a queue and a producer for it. Shared with
        /// <see cref="LiteDbConcurrencyBenchmarks"/> so the two suites build their fixtures the
        /// same way.
        /// </summary>
        internal static (QueueCreationContainer<LiteDbMessageQueueInit>, QueueContainer<LiteDbMessageQueueInit>,
            IProducerQueue<Event>) CreateQueue(string name, string connectionString)
        {
            var connection = new QueueConnection(name, connectionString);
            var creation = new QueueCreationContainer<LiteDbMessageQueueInit>();
            using (var creator = creation.GetQueueCreation<LiteDbMessageQueueCreation>(connection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            var container = new QueueContainer<LiteDbMessageQueueInit>();
            return (creation, container, container.CreateProducer<Event>(connection));
        }
    }
}
