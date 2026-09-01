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
using LiteDB;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Isolates the query a LiteDb de-queue runs to find the next message, against the index sets
    /// that could serve it.
    /// </summary>
    /// <remarks>
    /// <c>ReceiveMessageQueryHandler.DequeueRecord</c> filters the meta collection on
    /// <c>Status</c>, <c>HeartBeat</c>, <c>QueueProcessTime</c> and <c>ExpirationTime</c>, orders by
    /// <c>QueuedDateTime</c> and takes one.
    /// <para>
    /// <c>MetaDataTable.Create</c> builds <c>Id</c>, <c>QueueId</c> (unique), and — because both
    /// options are hard-coded true — <c>Status</c> and <c>HeartBeat</c>. It does not index
    /// <c>QueuedDateTime</c>, the field the query sorts by.
    /// </para>
    /// <para>
    /// LiteDB picks a single index per query. The question these rungs answer is which one it picks
    /// and what that costs: an equality seek on a field where every queued row has the same value
    /// selects the whole collection and still has to sort it, whereas walking the sort field's index
    /// can stop at the first row that matches. Depth is a parameter because that difference only
    /// shows up as the queue grows.
    /// </para>
    /// <para>
    /// The rungs measure the query alone. It mutates nothing, so it is repeatable — a real de-queue
    /// is not.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class LiteDbReceiveBenchmarks
    {
        /// <summary>How many messages are waiting in the queue.</summary>
        [Params(1_000, 10_000)]
        public int Depth { get; set; }

        private string _dir;
        private LiteDatabase _shipped;
        private LiteDatabase _withoutStatusHeartBeat;
        private LiteDatabase _shippedPlusQueuedDate;
        private LiteDatabase _queuedDateInstead;
        private LiteDatabase _dropStatusOnly;
        private LiteDatabase _dropHeartBeatOnly;
        private LiteDatabase _windowWorstCase;
        private LiteDatabase _insertShipped;
        private LiteDatabase _insertCandidate;
        private int _insertId;

        /// <summary>The fields the de-queue query touches.</summary>
        public sealed class Meta
        {
            public int Id { get; set; }
            public int QueueId { get; set; }
            public int Status { get; set; }
            public DateTime? HeartBeat { get; set; }
            public DateTime? QueueProcessTime { get; set; }
            public DateTime? ExpirationTime { get; set; }
            public DateTime QueuedDateTime { get; set; }
        }

        private const int Waiting = 0;
        private const int Processing = 1;

        /// <summary>Always built, whatever the options.</summary>
        private static void Core(ILiteCollection<Meta> col)
        {
            col.EnsureIndex(x => x.Id);
            col.EnsureIndex(x => x.QueueId, true);
        }

        /// <summary>What ships today under default options.</summary>
        private static void Shipped(ILiteCollection<Meta> col)
        {
            Core(col);
            col.EnsureIndex(x => x.Status);
            col.EnsureIndex(x => x.HeartBeat);
        }

        [GlobalSetup]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-litedb-recv", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);

            _shipped = Seed("shipped", Shipped);
            _withoutStatusHeartBeat = Seed("core", Core);
            _shippedPlusQueuedDate = Seed("plus", c => { Shipped(c); c.EnsureIndex(x => x.QueuedDateTime); });
            _queuedDateInstead = Seed("instead", c => { Core(c); c.EnsureIndex(x => x.QueuedDateTime); });
            _dropStatusOnly = Seed("dropstatus",
                c => { Core(c); c.EnsureIndex(x => x.HeartBeat); c.EnsureIndex(x => x.QueuedDateTime); });
            _dropHeartBeatOnly = Seed("dropheart",
                c => { Core(c); c.EnsureIndex(x => x.Status); c.EnsureIndex(x => x.QueuedDateTime); });

            //everything ahead of the last row deferred: the ordered walk cannot stop early and has
            //to page through the queue, which is where Skip gets expensive
            _windowWorstCase = Seed("winworst",
                c => { Shipped(c); c.EnsureIndex(x => x.QueuedDateTime); },
                (row, i, last) => { if (i < last) row.QueueProcessTime = DateTime.UtcNow.AddHours(1); });

            _insertShipped = Empty("ins-shipped", Shipped);
            //the proposal adds an index without removing any, so this is the real write-side cost
            _insertCandidate = Empty("ins-candidate", c => { Shipped(c); c.EnsureIndex(x => x.QueuedDateTime); });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            foreach (var db in new[] { _shipped, _withoutStatusHeartBeat, _shippedPlusQueuedDate,
                         _queuedDateInstead, _dropStatusOnly, _dropHeartBeatOnly, _windowWorstCase,
                         _insertShipped, _insertCandidate })
                db?.Dispose();

            try { Directory.Delete(_dir, true); }
            catch (IOException) { /* leave it behind */ }
            catch (UnauthorizedAccessException) { /* leave it behind */ }
        }

        #region finding the next message

        /// <summary>What the transport does today.</summary>
        [Benchmark(Baseline = true, Description = "as shipped: Status + HeartBeat indexed")]
        public int Shipped_AsIs() => Next(_shipped);

        /// <summary>
        /// The shipped indexes removed. Every queued row has the same <c>Status</c> and a null
        /// <c>HeartBeat</c>, so neither selects anything — this shows whether they are earning their
        /// keep on this query or getting in the way of it.
        /// </summary>
        [Benchmark(Description = "without Status + HeartBeat indexed")]
        public int WithoutStatusHeartBeat() => Next(_withoutStatusHeartBeat);

        /// <summary>Adding the sort field's index while leaving the others in place.</summary>
        [Benchmark(Description = "as shipped, plus QueuedDateTime indexed")]
        public int ShippedPlusQueuedDate() => Next(_shippedPlusQueuedDate);

        /// <summary>
        /// The candidate: the sort field indexed and the two non-selective ones gone, so the only
        /// index worth choosing is the one that can stop early.
        /// </summary>
        [Benchmark(Description = "QueuedDateTime indexed instead of Status + HeartBeat")]
        public int QueuedDateInstead() => Next(_queuedDateInstead);

        /// <summary>Keeping HeartBeat, which the monitor query uses, and dropping only Status.</summary>
        [Benchmark(Description = "QueuedDateTime added, Status dropped, HeartBeat kept")]
        public int DropStatusOnly() => Next(_dropStatusOnly);

        /// <summary>The mirror: keeping Status and dropping only HeartBeat.</summary>
        [Benchmark(Description = "QueuedDateTime added, HeartBeat dropped, Status kept")]
        public int DropHeartBeatOnly() => Next(_dropHeartBeatOnly);

        /// <summary>
        /// Keeps every index the transport has today, adds <c>QueuedDateTime</c>, and moves the
        /// eligibility predicates out of the query into memory.
        /// </summary>
        /// <remarks>
        /// The planner picks one index per query. Leaving only the sort field in the <c>Where</c>
        /// is what forces it to walk that index in order rather than seeking a field where every
        /// queued row has the same value. The predicates still run, just over a small ordered
        /// window instead of the whole collection - so the monitor query keeps its indexes and the
        /// de-queue stops scanning.
        /// </remarks>
        [Benchmark(Description = "ordered window, predicates in memory, all indexes kept")]
        public int OrderedWindow() => NextWindowedSkip(_shippedPlusQueuedDate);

        /// <summary>
        /// The same walk with no index on the sort field, which decides whether this needs a schema
        /// change or is purely a query change.
        /// </summary>
        [Benchmark(Description = "ordered window, without the QueuedDateTime index")]
        public int OrderedWindowNoIndex() => NextWindowedSkip(_shipped);

        /// <summary>
        /// The adversarial case: every row ahead of the last is deferred, so the walk pages through
        /// the whole queue. Skip is not free, so this is where the approach would fall over.
        /// </summary>
        [Benchmark(Description = "ordered window, all but one deferred")]
        public int OrderedWindowWorstCase() => NextWindowedSkip(_windowWorstCase);

        /// <summary>The seek pager on a normal queue.</summary>
        [Benchmark(Description = "ordered window by seek, all indexes kept")]
        public int OrderedWindowSeek() => NextWindowedSeek(_shippedPlusQueuedDate);

        /// <summary>
        /// Seeking on the primary key instead of the timestamp.
        /// </summary>
        /// <remarks>
        /// Two reasons this may be better. <c>QueuedDateTime</c> is <c>DateTime.UtcNow</c> at
        /// insert, so a batch can give many messages the same value - and a seek of
        /// <c>&gt; lastSeen</c> would step over every message that shares the boundary timestamp,
        /// losing them. <c>Id</c> is auto-increment, so it is unique, already in queue order, and
        /// already indexed - which would mean no schema change at all.
        /// </remarks>
        [Benchmark(Description = "ordered window by Id seek, no new index")]
        public int OrderedWindowIdSeek() => NextWindowedIdSeek(_shipped);

        /// <summary>The Id seek where the whole head of the queue is deferred.</summary>
        [Benchmark(Description = "ordered window by Id seek, all but one deferred")]
        public int OrderedWindowIdSeekWorstCase() => NextWindowedIdSeek(_windowWorstCase);

        /// <summary>Walks the queue in primary-key order, which is insertion order.</summary>
        private static int NextWindowedIdSeek(LiteDatabase db)
        {
            const int Window = 64;
            var col = db.GetCollection<Meta>("qmeta");
            var now = DateTime.UtcNow;
            var after = 0;

            while (true)
            {
                var page = col.Query()
                    .Where(x => x.Id > after)
                    .OrderBy(x => x.Id)
                    .Limit(Window)
                    .ToList();

                if (page.Count == 0) return 0;
                foreach (var row in page)
                    if (Eligible(row, now)) return 1;
                after = page[page.Count - 1].Id;
            }
        }

        /// <summary>The seek pager where the whole head of the queue is deferred.</summary>
        [Benchmark(Description = "ordered window by seek, all but one deferred")]
        public int OrderedWindowSeekWorstCase() => NextWindowedSeek(_windowWorstCase);

        /// <summary>
        /// Walks the queue in <c>QueuedDateTime</c> order using <c>Skip</c>, stopping at the first
        /// eligible row.
        /// </summary>
        private static int NextWindowedSkip(LiteDatabase db)
        {
            const int Window = 64;
            var col = db.GetCollection<Meta>("qmeta");
            var now = DateTime.UtcNow;
            var skip = 0;

            while (true)
            {
                var page = col.Query().OrderBy(x => x.QueuedDateTime).Skip(skip).Limit(Window).ToList();
                if (page.Count == 0) return 0;
                foreach (var row in page)
                    if (Eligible(row, now)) return 1;
                skip += Window;
            }
        }

        /// <summary>
        /// The same walk paged by value rather than by offset: each page seeks straight to where the
        /// last one ended.
        /// </summary>
        /// <remarks>
        /// <c>Skip</c> costs more the further in you are, so a queue whose head is all deferred
        /// degrades quadratically. Seeking on the same indexed field keeps every page the same
        /// price.
        /// </remarks>
        private static int NextWindowedSeek(LiteDatabase db)
        {
            const int Window = 64;
            var col = db.GetCollection<Meta>("qmeta");
            var now = DateTime.UtcNow;
            var after = DateTime.MinValue;

            while (true)
            {
                var page = col.Query()
                    .Where(x => x.QueuedDateTime > after)
                    .OrderBy(x => x.QueuedDateTime)
                    .Limit(Window)
                    .ToList();

                if (page.Count == 0) return 0;
                foreach (var row in page)
                    if (Eligible(row, now)) return 1;
                after = page[page.Count - 1].QueuedDateTime;
            }
        }

        /// <summary>
        /// The eligibility test the query used to do, moved into memory.
        /// </summary>
        /// <remarks>
        /// The conversions are not decoration. LiteDB hands back <see cref="DateTime"/> with
        /// <see cref="DateTimeKind.Local"/> even for values written as UTC, and comparing that to
        /// <c>UtcNow</c> compares raw ticks without converting - which reads a message deferred an
        /// hour ahead as ready to process. An earlier version of this benchmark did exactly that and
        /// reported a result that was fast because it was wrong.
        /// </remarks>
        private static bool Eligible(Meta row, DateTime nowUtc)
        {
            if (row.Status != Waiting || row.HeartBeat != null) return false;
            if (row.QueueProcessTime.HasValue && row.QueueProcessTime.Value.ToUniversalTime() >= nowUtc)
                return false;
            if (row.ExpirationTime.HasValue && row.ExpirationTime.Value.ToUniversalTime() <= nowUtc)
                return false;
            return true;
        }

        #endregion

        #region the heartbeat monitor query, which is why those indexes exist

        /// <summary>
        /// <c>FindRecordsToResetByHeartBeatQueryHandler</c>: in-flight rows whose heartbeat has gone
        /// stale. Unlike the de-queue, its <c>Status</c> predicate <em>is</em> selective, which is
        /// the case for keeping the index — so dropping one has to be judged against this too.
        /// </summary>
        [Benchmark(Description = "monitor: stale heartbeats, indexes as shipped")]
        public int MonitorShipped() => Stale(_shipped);

        /// <summary>The same query with both indexes dropped.</summary>
        [Benchmark(Description = "monitor: stale heartbeats, QueuedDateTime instead")]
        public int MonitorQueuedDateInstead() => Stale(_queuedDateInstead);

        /// <summary>The same query keeping HeartBeat only.</summary>
        [Benchmark(Description = "monitor: stale heartbeats, Status dropped")]
        public int MonitorDropStatusOnly() => Stale(_dropStatusOnly);

        private static int Stale(LiteDatabase db)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            return db.GetCollection<Meta>("qmeta").Query()
                .Where(x => x.Status == Processing)
                .Where(x => x.HeartBeat.HasValue && x.HeartBeat.Value < cutoff)
                .Limit(50)
                .ToList().Count;
        }

        #endregion

        #region what the indexes cost to carry

        /// <summary>Insert cost with the shipped index set.</summary>
        [Benchmark(Description = "insert one message, indexes as shipped")]
        public void InsertShipped() => Insert(_insertShipped);

        /// <summary>Insert cost with the candidate index set.</summary>
        [Benchmark(Description = "insert one message, shipped + QueuedDateTime")]
        public void InsertCandidate() => Insert(_insertCandidate);

        #endregion

        private LiteDatabase Empty(string name, Action<ILiteCollection<Meta>> index)
        {
            var db = new LiteDatabase($"Filename={Path.Combine(_dir, name + Depth + ".db")};Connection=direct;");
            index(db.GetCollection<Meta>("qmeta"));
            return db;
        }

        private LiteDatabase Seed(string name, Action<ILiteCollection<Meta>> index,
            Action<Meta, int, int> tweak = null)
        {
            var db = new LiteDatabase($"Filename={Path.Combine(_dir, name + Depth + ".db")};Connection=direct;");
            var col = db.GetCollection<Meta>("qmeta");
            var start = DateTime.UtcNow.AddHours(-1);
            var rows = new List<Meta>(Depth);
            for (var i = 0; i < Depth; i++)
            {
                //one row in a hundred is in flight with a stale heartbeat, which is roughly what a
                //queue with a handful of workers looks like and is what the monitor query hunts for
                var inFlight = i % 100 == 0;
                var row = new Meta
                {
                    QueueId = i + 1,
                    Status = inFlight ? Processing : Waiting,
                    HeartBeat = inFlight ? start.AddMinutes(-5) : null,
                    QueuedDateTime = start.AddMilliseconds(i)
                };
                tweak?.Invoke(row, i, Depth - 1);
                rows.Add(row);
            }
            col.InsertBulk(rows);
            index(col);
            return db;
        }

        /// <summary>QueueId is uniquely indexed, so every insert needs its own.</summary>
        private void Insert(LiteDatabase db)
        {
            db.GetCollection<Meta>("qmeta").Insert(new Meta
            {
                QueueId = ++_insertId,
                Status = Waiting,
                QueuedDateTime = DateTime.UtcNow
            });
        }

        private static int Next(LiteDatabase db)
        {
            var results = db.GetCollection<Meta>("qmeta").Query()
                .Where(x => x.Status == Waiting)
                .Where(x => x.HeartBeat == null)
                .Where(x => x.QueueProcessTime == null || x.QueueProcessTime < DateTime.UtcNow)
                .Where(x => x.ExpirationTime == null || x.ExpirationTime > DateTime.UtcNow)
                .OrderBy(x => x.QueuedDateTime)
                .Limit(1)
                .ToList();
            return results.Count;
        }
    }
}
