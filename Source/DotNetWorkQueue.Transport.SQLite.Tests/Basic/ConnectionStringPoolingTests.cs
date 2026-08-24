using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class ConnectionStringPoolingTests
    {
        private const string FileDb = @"Data Source=c:\test\temp.db3;Version=3;";

        private static bool PoolingOf(string connectionString) =>
            new SQLiteConnectionStringBuilder(connectionString).Pooling;

        private static bool SpecifiesPooling(string connectionString) =>
            new SQLiteConnectionStringBuilder(connectionString).ContainsKey("Pooling");

        [TestMethod]
        public void Apply_EnablesPooling_WhenCallerDidNotSpecify()
        {
            var result = ConnectionStringPooling.Apply(FileDb, false);

            Assert.IsTrue(PoolingOf(result), "pooling should be enabled by default for a file database");
        }

        [TestMethod]
        public void Apply_PreservesTheRestOfTheConnectionString()
        {
            var result = ConnectionStringPooling.Apply(FileDb, false);
            var builder = new SQLiteConnectionStringBuilder(result);

            Assert.AreEqual(@"c:\test\temp.db3", builder.DataSource);
        }

        [TestMethod]
        public void Apply_RespectsExplicitPoolingTrue()
        {
            var result = ConnectionStringPooling.Apply(FileDb + "Pooling=True;", false);

            Assert.IsTrue(PoolingOf(result));
        }

        [TestMethod]
        public void Apply_RespectsExplicitPoolingFalse()
        {
            //a caller who deliberately turned pooling off must keep it off
            var result = ConnectionStringPooling.Apply(FileDb + "Pooling=False;", false);

            Assert.IsFalse(PoolingOf(result));
        }

        [TestMethod]
        public void Apply_RespectsExplicitPoolingFalse_RegardlessOfCasing()
        {
            var result = ConnectionStringPooling.Apply(FileDb + "pooling=false;", false);

            Assert.IsFalse(PoolingOf(result));
        }

        [TestMethod]
        public void Apply_DoesNotPool_WhenHoldingAnInMemoryDatabaseOpen()
        {
            //the hold connection is never released, so pooling it would serve no purpose
            var result = ConnectionStringPooling.Apply(FileDb, true);

            Assert.IsFalse(SpecifiesPooling(result));
            Assert.AreEqual(FileDb, result);
        }

        [TestMethod]
        public void Apply_DoesNotPool_InMemoryDatabase()
        {
            const string inMemory = "Data Source=:memory:;Version=3;";

            var result = ConnectionStringPooling.Apply(inMemory, false);

            Assert.IsFalse(SpecifiesPooling(result));
            Assert.AreEqual(inMemory, result);
        }

        [TestMethod]
        public void Apply_DoesNotPool_SharedCacheInMemoryDatabase()
        {
            //a pooled connection would keep a shared-cache in-memory database alive past disposal
            const string sharedMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

            var result = ConnectionStringPooling.Apply(sharedMemory, false);

            Assert.IsFalse(SpecifiesPooling(result));
            Assert.AreEqual(sharedMemory, result);
        }

        [TestMethod]
        public void Apply_PreservesTheCallersStringVerbatim()
        {
            //Regression guard. An earlier implementation returned SQLiteConnectionStringBuilder's
            //round-tripped string, which discards input it cannot parse: this exact value came
            //back as "pooling=True", destroying the caller's connection string. Validation belongs
            //elsewhere, so a string we misjudge must still fail the way it would have without us.
            const string malformed = "this is not a connection string=;;;";

            var result = ConnectionStringPooling.Apply(malformed, false);

            StringAssert.StartsWith(result, malformed);
        }

        [TestMethod]
        public void Apply_DoesNotRewriteKeywordsTheCallerSupplied()
        {
            //casing and quoting of the caller's own keywords must survive untouched
            const string original = @"Data Source=c:\test\temp.db3;Version=3;Journal Mode=WAL;SomeUnknownKeyword=42;";

            var result = ConnectionStringPooling.Apply(original, false);

            StringAssert.StartsWith(result, original);
            Assert.IsTrue(PoolingOf(result));
        }

        [TestMethod]
        public void Apply_ReturnsNullOrEmptyUnchanged()
        {
            Assert.IsNull(ConnectionStringPooling.Apply(null, false));
            Assert.AreEqual(string.Empty, ConnectionStringPooling.Apply(string.Empty, false));
        }

        [TestMethod]
        public void Apply_IsStableAcrossRepeatedCalls()
        {
            //the result is cached; a second call must not append pooling twice or drift
            var first = ConnectionStringPooling.Apply(FileDb, false);
            var second = ConnectionStringPooling.Apply(FileDb, false);

            Assert.AreEqual(first, second);
        }
    }
}
