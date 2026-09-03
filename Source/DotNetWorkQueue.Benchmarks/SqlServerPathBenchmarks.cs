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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler;
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

        private QueueCreationContainer<SqlServerMessageQueueInit> _creation;
        private QueueContainer<SqlServerMessageQueueInit> _container;
        private IProducerQueue<Event> _producer;
        private QueueConnection _queueConnection;
        private string _payload;
        private List<Event> _batch;
        private const int BatchSize = 100;

        //collaborators for the SQL-generation rung, taken from the producer's own container so it
        //measures what the send path really calls rather than a stand-in
        private ITableNameHelper _tableNameHelper;
        private IHeaders _queueHeaders;
        private SqlServerMessageQueueTransportOptions _options;
        private IAdditionalMessageData _messageData;
        private IMessage _message;

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

            _payload = new string('x', PayloadBytes);
            _batch = new List<Event>(BatchSize);
            for (var i = 0; i < BatchSize; i++) _batch.Add(new Event { Body = _payload });

            _queueConnection = new QueueConnection("benchSqlServer" + suffix, _connectionString);
            _creation = new QueueCreationContainer<SqlServerMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<SqlServerMessageQueueCreation>(_queueConnection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }
            _container = new QueueContainer<SqlServerMessageQueueInit>();
            _producer = _container.CreateProducer<Event>(_queueConnection);

            var container = ConsumerInternals.ContainerOf(_container);
            _tableNameHelper = container.GetInstance<ITableNameHelper>();
            _queueHeaders = container.GetInstance<IHeaders>();
            _options = container.GetInstance<ISqlServerMessageQueueTransportOptionsFactory>().Create();

            //the correlation id is not optional here: BuildMetaCommand reads
            //data.CorrelationId.Id.Value, and the real send path fills it in HeaderSetup before
            //ever reaching the handler
            _messageData = new AdditionalMessageData
            {
                CorrelationId = container.GetInstance<ICorrelationIdFactory>().Create()
            };
            _message = container.GetInstance<IMessageFactory>()
                .Create(new Event { Body = _payload }, null);
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
            _producer?.Dispose();
            _container?.Dispose();
            if (_queueConnection != null)
            {
                try
                {
                    using var creator = _creation.GetQueueCreation<SqlServerMessageQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (SqlException)
                {
                    //a queue left behind is not worth failing a run over
                }
            }
            _creation?.Dispose();

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

        /// <summary>
        /// The whole send, as a caller experiences it. Against the four-round-trip raw row, what
        /// is left is the library.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue SQL Server send (end to end)")]
        public void Transport_Send()
        {
            var result = _producer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        /// <summary>
        /// The batch path from 0.9.41, reported per batch - divide by <see cref="BatchSize"/> for
        /// the per-message cost.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue SQL Server batch send (100 messages)")]
        public void Transport_SendBatch()
        {
            foreach (var result in _producer.Send(_batch))
            {
                if (result.HasError)
                    throw result.SendingException ?? new InvalidOperationException("batch send failed");
            }
        }

        /// <summary>
        /// The meta insert's SQL, assembled per send. The other question this issue asks: on
        /// SQLite, generating the de-queue script was 91% of everything a de-queue allocated.
        /// </summary>
        [Benchmark(Description = "meta insert SQL, built per send (no round trip)")]
        public int MetaSql_Build()
        {
            using var command = _heldConnection.CreateCommand();
            SendMessage.BuildMetaCommand(command, _tableNameHelper, _queueHeaders, _messageData,
                _message, 1, _options, null, TimeSpan.Zero);
            return command.CommandText.Length;
        }

        private void AddBodyParameters(SqlCommand command)
        {
            command.Parameters.Add(BodyParameter, SqlDbType.VarBinary, -1).Value = _body;
            command.Parameters.Add(HeadersParameter, SqlDbType.VarBinary, -1).Value = _headers;
        }

        /// <summary>The message body these rungs send.</summary>
        public sealed class Event
        {
            public string Body { get; set; }
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
