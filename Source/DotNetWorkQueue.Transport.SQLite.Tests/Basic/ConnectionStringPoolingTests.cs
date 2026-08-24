using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class ConnectionStringPoolingTests
    {
        private const string FileDb = @"Data Source=c:\test\temp.db3;Version=3;";

        private static bool PoolingOf(string connectionString) =>
            new SQLiteConnectionStringBuilder(connectionString).Pooling;

        private static bool SpecifiesPooling(string connectionString) =>
            new SQLiteConnectionStringBuilder(connectionString).ContainsKey("Pooling");

        [Fact]
        public void Apply_EnablesPooling_WhenCallerDidNotSpecify()
        {
            var result = ConnectionStringPooling.Apply(FileDb, false);

            Assert.True(PoolingOf(result), "pooling should be enabled by default for a file database");
        }

        [Fact]
        public void Apply_PreservesTheRestOfTheConnectionString()
        {
            var result = ConnectionStringPooling.Apply(FileDb, false);
            var builder = new SQLiteConnectionStringBuilder(result);

            Assert.Equal(@"c:\test\temp.db3", builder.DataSource);
        }

        [Fact]
        public void Apply_RespectsExplicitPoolingTrue()
        {
            var result = ConnectionStringPooling.Apply(FileDb + "Pooling=True;", false);

            Assert.True(PoolingOf(result));
        }

        [Fact]
        public void Apply_RespectsExplicitPoolingFalse()
        {
            //a caller who deliberately turned pooling off must keep it off
            var result = ConnectionStringPooling.Apply(FileDb + "Pooling=False;", false);

            Assert.False(PoolingOf(result));
        }

        [Fact]
        public void Apply_RespectsExplicitPoolingFalse_RegardlessOfCasing()
        {
            var result = ConnectionStringPooling.Apply(FileDb + "pooling=false;", false);

            Assert.False(PoolingOf(result));
        }

        [Fact]
        public void Apply_DoesNotPool_WhenHoldingAnInMemoryDatabaseOpen()
        {
            //the hold connection is never released, so pooling it would serve no purpose
            var result = ConnectionStringPooling.Apply(FileDb, true);

            Assert.False(SpecifiesPooling(result));
            Assert.Equal(FileDb, result);
        }

        [Fact]
        public void Apply_DoesNotPool_InMemoryDatabase()
        {
            const string inMemory = "Data Source=:memory:;Version=3;";

            var result = ConnectionStringPooling.Apply(inMemory, false);

            Assert.False(SpecifiesPooling(result));
            Assert.Equal(inMemory, result);
        }

        [Fact]
        public void Apply_DoesNotPool_SharedCacheInMemoryDatabase()
        {
            //a pooled connection would keep a shared-cache in-memory database alive past disposal
            const string sharedMemory = "FullUri=file:test.db3?mode=memory&cache=shared;Version=3;";

            var result = ConnectionStringPooling.Apply(sharedMemory, false);

            Assert.False(SpecifiesPooling(result));
            Assert.Equal(sharedMemory, result);
        }

        [Fact]
        public void Apply_PreservesTheCallersStringVerbatim()
        {
            //Regression guard. An earlier implementation returned SQLiteConnectionStringBuilder's
            //round-tripped string, which discards input it cannot parse: this exact value came
            //back as "pooling=True", destroying the caller's connection string. Validation belongs
            //elsewhere, so a string we misjudge must still fail the way it would have without us.
            const string malformed = "this is not a connection string=;;;";

            var result = ConnectionStringPooling.Apply(malformed, false);

            Assert.StartsWith(malformed, result);
        }

        [Fact]
        public void Apply_DoesNotRewriteKeywordsTheCallerSupplied()
        {
            //casing and quoting of the caller's own keywords must survive untouched
            const string original = @"Data Source=c:\test\temp.db3;Version=3;Journal Mode=WAL;SomeUnknownKeyword=42;";

            var result = ConnectionStringPooling.Apply(original, false);

            Assert.StartsWith(original, result);
            Assert.True(PoolingOf(result));
        }

        [Fact]
        public void Apply_ReturnsNullOrEmptyUnchanged()
        {
            Assert.Null(ConnectionStringPooling.Apply(null, false));
            Assert.Equal(string.Empty, ConnectionStringPooling.Apply(string.Empty, false));
        }

        [Fact]
        public void Apply_IsStableAcrossRepeatedCalls()
        {
            //the result is cached; a second call must not append pooling twice or drift
            var first = ConnectionStringPooling.Apply(FileDb, false);
            var second = ConnectionStringPooling.Apply(FileDb, false);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Apply_StaysCorrectBeyondTheCacheCap()
        {
            //The cache is bounded so a caller generating connection strings dynamically cannot
            //grow it for the life of the process. Past the cap entries are simply rebuilt, so the
            //answers must stay correct rather than degrade.
            for (var i = 0; i < 300; i++)
            {
                var unique = $@"Data Source=c:\test\cap-{i}.db3;Version=3;";

                var result = ConnectionStringPooling.Apply(unique, false);

                Assert.StartsWith(unique, result);
                Assert.True(PoolingOf(result), $"pooling should still be enabled for entry {i}");
            }
        }
    }
}
