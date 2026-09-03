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
using System.Data;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a single SQL Server send. Unlike the embedded transports, a round trip dominates
    /// here, so the ladder is built to count round trips rather than to chase microseconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An ordinary send makes four: <c>BeginTransaction</c>, the body insert (which returns the
    /// identity), the meta insert, and <c>Commit</c> - plus whatever the pooled connection's reset
    /// costs. <see cref="RawInsert_OneRoundTrip"/> does the identical work as a single batch with
    /// the transaction and the identity handled server-side, so the difference between it and
    /// <see cref="RawInsert_DnwqShape"/> is what the extra three round trips are worth.
    /// </para>
    /// <para>
    /// <see cref="MetaSql_Build"/> is the other question this issue asks: the meta insert's SQL is
    /// assembled with a <c>StringBuilder</c> on every send, and on SQLite generating the de-queue
    /// script turned out to be 91% of everything a de-queue allocated.
    /// </para>
    /// <para>
    /// <b>Requires a SQL Server instance.</b> Set <c>DNWQ_SQLSERVER_CONNECTION</c> to a connection
    /// string for a database the benchmark may create and drop tables in. It is read from the
    /// environment rather than the integration tests' <c>connectionstring.txt</c> because the
    /// harness is run from a copied output directory, where that file is not on disk - and because
    /// a benchmark should not be the thing that reads a credential file.
    /// </para>
    /// </remarks>
    /// <para>
    /// Every rung starts from empty tables, and an iteration is a fixed, small number of
    /// invocations. Without that the ladder measures the order it ran in: the rungs insert and
    /// never delete, BenchmarkDotNet runs them in declaration order, and the later ones pay the
    /// data-file growth the earlier ones caused. The first version of this suite reported the
    /// held-connection rung - which runs last - as the *slowest*, at 14.1 ms against 8.4 ms for
    /// the same work on a pooled connection, which is backwards.
    /// </para>
    [MemoryDiagnoser]
    [InvocationCount(16)]
    public class SqlServerPathBenchmarks
    {
        private const int PayloadBytes = 256;
        private const string BodyParameter = "@Body";
        private const string HeadersParameter = "@Headers";
        private const string CorrelationParameter = "@CorrelationID";

        private string _connectionString;
        private string _queueTable;
        private string _metaTable;

        private byte[] _body;
        private byte[] _headers;

        private SqlConnection _heldConnection;

        private string _insertBodySql;
        private string _insertMetaSql;
        private string _oneRoundTripSql;

        [GlobalSetup]
        public void Setup()
        {
            _connectionString = Environment.GetEnvironmentVariable("DNWQ_SQLSERVER_CONNECTION");
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_SQLSERVER_CONNECTION to a SQL Server connection string. See the class remarks.");

            _body = new byte[PayloadBytes];
            _headers = new byte[64];

            var suffix = Guid.NewGuid().ToString("N");
            _queueTable = $"bench_q_{suffix}";
            _metaTable = $"bench_qmeta_{suffix}";

            Execute($@"
CREATE TABLE [{_queueTable}] (
    QueueID bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Body varbinary(max) NULL,
    Headers varbinary(max) NULL);
CREATE TABLE [{_metaTable}] (
    QueueID bigint NOT NULL PRIMARY KEY,
    CorrelationID uniqueidentifier NOT NULL,
    QueuedDateTime datetime NOT NULL,
    Status int NOT NULL);");

            //the transport's own statement, verbatim
            _insertBodySql =
                $"Insert into [{_queueTable}] (Body, Headers) VALUES (@Body, @Headers) select SCOPE_IDENTITY() ";
            _insertMetaSql =
                $"Insert into [{_metaTable}] (QueueID, CorrelationID, QueuedDateTime, Status) " +
                "VALUES (@QueueID, @CorrelationID, GetUTCDate(), 0)";

            //the same work, one round trip: the transaction and the identity never leave the server
            _oneRoundTripSql = $@"
SET NOCOUNT ON;
DECLARE @id bigint;
BEGIN TRANSACTION;
INSERT INTO [{_queueTable}] (Body, Headers) VALUES (@Body, @Headers);
SET @id = SCOPE_IDENTITY();
INSERT INTO [{_metaTable}] (QueueID, CorrelationID, QueuedDateTime, Status)
VALUES (@id, @CorrelationID, GetUTCDate(), 0);
COMMIT TRANSACTION;
SELECT @id;";

            _heldConnection = new SqlConnection(_connectionString);
            _heldConnection.Open();
        }

        /// <summary>
        /// Empties the tables so every rung sees the same starting size. Not measured.
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            Execute($"TRUNCATE TABLE [{_metaTable}]; TRUNCATE TABLE [{_queueTable}];");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _heldConnection?.Dispose();
            try
            {
                Execute($"DROP TABLE IF EXISTS [{_metaTable}]; DROP TABLE IF EXISTS [{_queueTable}];");
            }
            catch (SqlException)
            {
                //a table left behind is not worth failing a run over
            }
            SqlConnection.ClearAllPools();
        }

        /// <summary>
        /// The real floor, and it has to come first: one round trip that does no work at all.
        /// Every other row is meaningless until this is known - a ladder whose rungs are all
        /// smaller than the link they run over is measuring the link.
        /// </summary>
        [Benchmark(Description = "round trip only: SELECT 1, pooled connection")]
        public int Roundtrip_SelectOne()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            return (int)command.ExecuteScalar();
        }

        /// <summary>The same on a connection already open, so the pool is out of the picture.</summary>
        [Benchmark(Description = "round trip only: SELECT 1, held connection")]
        public int Roundtrip_SelectOneHeld()
        {
            using var command = _heldConnection.CreateCommand();
            command.CommandText = "SELECT 1";
            return (int)command.ExecuteScalar();
        }

        /// <summary>The floor: one insert, one round trip, no explicit transaction.</summary>
        [Benchmark(Baseline = true, Description = "raw: 1 insert, pooled connection")]
        public long RawInsert_OneStatement()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = _insertBodySql;
            AddBodyParameters(command);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>
        /// What the transport does: a pooled connection, an explicit transaction, the body insert
        /// that returns the identity, the meta insert, and a commit. Four round trips.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, 4 round trips")]
        public long RawInsert_DnwqShape()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var trans = connection.BeginTransaction();

            long id;
            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                command.CommandText = _insertBodySql;
                AddBodyParameters(command);
                id = Convert.ToInt64(command.ExecuteScalar());
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = trans;
                command.CommandText = _insertMetaSql;
                command.Parameters.Add("@QueueID", SqlDbType.BigInt).Value = id;
                command.Parameters.Add(CorrelationParameter, SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
                command.ExecuteNonQuery();
            }

            trans.Commit();
            return id;
        }

        /// <summary>
        /// The identical work as one batch. Against the row above, this is what the extra three
        /// round trips cost.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, 1 round trip (same work)")]
        public long RawInsert_OneRoundTrip()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = _oneRoundTripSql;
            AddBodyParameters(command);
            command.Parameters.Add(CorrelationParameter, SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>The same one-round-trip batch on a connection that is already open.</summary>
        [Benchmark(Description = "raw: DNWQ shape, 1 round trip, held connection")]
        public long RawInsert_OneRoundTripHeldConnection()
        {
            using var command = _heldConnection.CreateCommand();
            command.CommandText = _oneRoundTripSql;
            AddBodyParameters(command);
            command.Parameters.Add(CorrelationParameter, SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>Acquiring and returning a pooled connection, with no work done on it.</summary>
        [Benchmark(Description = "pooled connection open + close, no work")]
        public void Connection_OpenClose()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
        }

        private void AddBodyParameters(SqlCommand command)
        {
            command.Parameters.Add(BodyParameter, SqlDbType.VarBinary, -1).Value = _body;
            command.Parameters.Add(HeadersParameter, SqlDbType.VarBinary, -1).Value = _headers;
        }

        private void Execute(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }
}
