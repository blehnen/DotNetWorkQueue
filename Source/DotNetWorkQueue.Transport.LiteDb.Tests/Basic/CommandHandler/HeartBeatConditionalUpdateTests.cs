using System;
using LiteDB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DotNetWorkQueue.Transport.LiteDb.Schema;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic.CommandHandler
{
    /// <summary>
    /// The two properties SendHeartBeatCommandHandler depends on when it refreshes a claim.
    ///
    /// It updates through UpdateMany with the ownership test in the predicate, rather than reading the
    /// record and writing it back: Update() writes by document id and re-tests nothing, and LiteDb's
    /// transactions do not hold the record while that happens - the same weakness that forced queue
    /// creation to be serialised with a lock of our own in #318.
    ///
    /// Both properties below are LiteDb's behaviour rather than ours, so they are worth pinning: an
    /// upgrade that changed either one would make heartbeats either lose the rest of the record or stop
    /// detecting a lost claim, and neither failure announces itself (GitHub #328).
    /// </summary>
    [TestClass]
    public class HeartBeatConditionalUpdateTests
    {
        private static readonly DateTime Claimed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void AConditionalUpdate_ChangesTheHeartbeatAndNothingElse()
        {
            using (var db = new LiteDatabase(":memory:"))
            {
                var col = db.GetCollection<MetaDataTable>("meta");
                var correlation = Guid.NewGuid();
                col.Insert(new MetaDataTable
                {
                    QueueId = 7,
                    HeartBeat = Claimed,
                    CorrelationId = correlation,
                    Status = QueueStatuses.Processing,
                    QueuedDateTime = Claimed.AddMinutes(-5),
                    Route = "a-route"
                });

                var next = Claimed.AddSeconds(30);
                var updated = col.UpdateMany(x => new MetaDataTable { HeartBeat = next },
                    x => x.QueueId == 7 && x.HeartBeat == Claimed);

                Assert.AreEqual(1, updated, "the beat did not land on a record it should own");

                var after = col.FindOne(x => x.QueueId == 7);
                Assert.AreEqual(next, after.HeartBeat.Value.ToUniversalTime(),
                    "the heartbeat was not moved on");

                //the reason this test exists: a transform naming one field must not blank the others
                Assert.AreEqual(correlation, after.CorrelationId, "the correlation id was lost by a heartbeat");
                Assert.AreEqual(QueueStatuses.Processing, after.Status, "the status was lost by a heartbeat");
                Assert.AreEqual("a-route", after.Route, "the route was lost by a heartbeat");
                Assert.AreEqual(Claimed.AddMinutes(-5), after.QueuedDateTime.ToUniversalTime(),
                    "the queued date was lost by a heartbeat");
            }
        }

        [TestMethod]
        public void AConditionalUpdate_WhoseExpectedValueIsStale_ChangesNothing()
        {
            using (var db = new LiteDatabase(":memory:"))
            {
                var col = db.GetCollection<MetaDataTable>("meta");
                col.Insert(new MetaDataTable
                {
                    QueueId = 7,
                    HeartBeat = Claimed,
                    Status = QueueStatuses.Processing
                });

                //somebody else has beaten on it since - which is what a reset and re-claim looks like
                var somebodyElse = Claimed.AddSeconds(45);
                col.UpdateMany(x => new MetaDataTable { HeartBeat = somebodyElse }, x => x.QueueId == 7);

                var updated = col.UpdateMany(x => new MetaDataTable { HeartBeat = Claimed.AddSeconds(60) },
                    x => x.QueueId == 7 && x.HeartBeat == Claimed);

                Assert.AreEqual(0, updated,
                    "a beat carrying a stale expectation still landed, so a worker would refresh a claim that is no longer its own");

                var after = col.FindOne(x => x.QueueId == 7);
                Assert.AreEqual(somebodyElse, after.HeartBeat.Value.ToUniversalTime(),
                    "the stale beat overwrote the current owner's heartbeat");
            }
        }
    }
}
