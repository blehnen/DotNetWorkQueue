using System.Text;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    /// <summary>
    /// Building identifiers that survive PostgreSQL's 63 byte limit (GitHub #375).
    /// </summary>
    [TestClass]
    public class PostgreSqlIdentifierTests
    {
        [TestMethod]
        public void AnIdentifierThatFits_IsLeftAlone()
        {
            //the ordinary case has to stay readable in the database, and has to keep matching what
            //earlier versions produced
            const string name = "IX_QueueIDExceptionTypeMyQueueErrorTracking";
            Assert.AreEqual(name, PostgreSqlIdentifier.Shorten(name));
        }

        [TestMethod]
        public void AnIdentifierAtExactlyTheLimit_IsLeftAlone()
        {
            var name = new string('a', PostgreSqlIdentifier.MaxLengthInBytes);
            Assert.AreEqual(name, PostgreSqlIdentifier.Shorten(name));
        }

        [TestMethod]
        public void AnOverLongIdentifier_IsBroughtWithinTheLimit()
        {
            var name = "IX_QueueIDExceptionType" + new string('a', 60) + "ErrorTracking";
            var shortened = PostgreSqlIdentifier.Shorten(name);

            Assert.IsLessThanOrEqualTo(PostgreSqlIdentifier.MaxLengthInBytes,
                Encoding.UTF8.GetByteCount(shortened));
        }

        [TestMethod]
        public void TwoNamesSharingALongPrefix_DoNotShortenToTheSameThing()
        {
            //the whole point. PostgreSQL truncates rather than rejecting, so two of these used to
            //become one identifier and the second CREATE failed with 42P07
            var prefix = "IX_QueueIDExceptionType" + new string('a', 60);
            var first = PostgreSqlIdentifier.Shorten(prefix + "aaaErrorTracking");
            var second = PostgreSqlIdentifier.Shorten(prefix + "bbbErrorTracking");

            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(first.Substring(0, PostgreSqlIdentifier.MaxLengthInBytes),
                second.Substring(0, PostgreSqlIdentifier.MaxLengthInBytes),
                "they differ only past the point PostgreSQL would cut them");
        }

        [TestMethod]
        public void TheSameNameShortensTheSameWayEveryTime()
        {
            //string.GetHashCode is randomised per process, so a hash taken from it would give the
            //queue a different index name on every run
            var name = "IX_QueueIDExceptionType" + new string('a', 60) + "ErrorTracking";
            Assert.AreEqual(PostgreSqlIdentifier.Shorten(name), PostgreSqlIdentifier.Shorten(name));
        }

        [TestMethod]
        public void EveryIdentifierThisTransportBuilds_FitsAtTheQueueNameLimit()
        {
            //nine of the thirteen overflowed before this, the worst leaving 27 characters of the queue
            //name. Asserted together so a new table or index cannot quietly reintroduce the problem.
            var queue = new string('q', 51);
            string[] identifiers =
            {
                "PK_" + queue,
                "PK_" + queue + "Configuration",
                "PK_" + queue + "Status",
                "PK_" + queue + "MetaData",
                "IX_DeQueue" + queue + "MetaData",
                "IX_HeartBeat" + queue + "MetaData",
                "PK_" + queue + "ErrorTracking",
                "IX_QueueID" + queue + "ErrorTracking",
                "IX_QueueIDExceptionType" + queue + "ErrorTracking",
                "PK_" + queue + "MetaDataErrors",
                "PK_" + queue + "History",
                "IX_" + queue + "History_QueueID",
                "IX_" + queue + "History_Status_Completed"
            };

            foreach (var identifier in identifiers)
            {
                var shortened = PostgreSqlIdentifier.Shorten(identifier);
                Assert.IsLessThanOrEqualTo(PostgreSqlIdentifier.MaxLengthInBytes,
                    Encoding.UTF8.GetByteCount(shortened),
                    $"{identifier} shortened to {shortened}");
            }
        }

        [TestMethod]
        public void NullOrEmpty_IsReturnedUnchanged()
        {
            Assert.IsNull(PostgreSqlIdentifier.Shorten(null));
            Assert.AreEqual(string.Empty, PostgreSqlIdentifier.Shorten(string.Empty));
        }
    }
}
