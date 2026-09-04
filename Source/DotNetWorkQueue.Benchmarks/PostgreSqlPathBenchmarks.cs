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
using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes a single PostgreSQL <c>Send</c>, the way <see cref="SqlServerPathBenchmarks"/>
    /// does for SQL Server. The question #232 inherits from #231: an ordinary send makes four
    /// round trips, and this is what they cost here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rungs exist to answer one question before any transport code is written: is the round
    /// trip worth collapsing on <em>this</em> transport? The SQL Server answer does not carry over
    /// on its own - that ladder found the write dominates a round trip seventeen to one, and the
    /// same could be true or false here.
    /// </para>
    /// <para>
    /// PostgreSQL can express the whole send as a single <b>statement</b> rather than a batch,
    /// using data-modifying CTEs and <c>RETURNING</c>. That matters beyond speed: a single
    /// statement is atomic on its own, so the collapsed form needs no explicit transaction and
    /// cannot reproduce the failure mode SQL Server's batch had, where an error that does not
    /// abort the transaction reaches the commit. The <c>1 round trip</c> rung below is that shape.
    /// </para>
    /// <para>
    /// <b>Requires a PostgreSQL instance</b> via <c>DNWQ_POSTGRES_CONNECTION</c>, for a database
    /// the harness may create and drop tables in.
    /// </para>
    /// <para>
    /// Every rung starts from empty tables and an iteration is a fixed, small number of
    /// invocations - the same precaution <see cref="SqlServerPathBenchmarks"/> documents, where
    /// omitting it made the ladder measure the order it ran in rather than the work.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [InvocationCount(16)]
    public class PostgreSqlPathBenchmarks
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

        private QueueCreationContainer<PostgreSqlMessageQueueInit> _creation;
        private QueueContainer<PostgreSqlMessageQueueInit> _container;
        private IProducerQueue<Event> _producer;
        private QueueConnection _queueConnection;
        private string _payload;

        private string _insertBodySql;
        private string _insertMetaSql;
        private string _oneRoundTripSql;

        [GlobalSetup]
        public void Setup()
        {
            _connectionString = Environment.GetEnvironmentVariable("DNWQ_POSTGRES_CONNECTION");
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_POSTGRES_CONNECTION to a PostgreSQL connection string. See the class remarks.");

            _body = new byte[PayloadBytes];
            _headers = new byte[64];
            _payload = new string('x', PayloadBytes);

            var suffix = Guid.NewGuid().ToString("N");
            _queueTable = $"bench_q_{suffix}";
            _metaTable = $"bench_qmeta_{suffix}";

            Execute($@"
CREATE TABLE {_queueTable} (
    QueueID bigserial NOT NULL PRIMARY KEY,
    Body bytea NULL,
    Headers bytea NULL);
CREATE TABLE {_metaTable} (
    QueueID bigint NOT NULL PRIMARY KEY,
    CorrelationID uuid NOT NULL,
    QueuedDateTime timestamp NOT NULL,
    Status int NOT NULL);");

            //the transport's own statement shape, verbatim - note lastval() rather than RETURNING
            _insertBodySql =
                $"Insert into {_queueTable} (Body, Headers) VALUES (@Body, @Headers); SELECT lastval(); ";

            _insertMetaSql =
                $"Insert into {_metaTable} (QueueID, CorrelationID, QueuedDateTime, Status) " +
                "VALUES (@QueueID, @CorrelationID, now() at time zone 'utc', 0)";

            //the same work as one statement. Data-modifying CTEs run in a single snapshot and the
            //statement is atomic on its own, so there is no BEGIN/COMMIT here to match.
            _oneRoundTripSql = $@"
WITH b AS (
    INSERT INTO {_queueTable} (Body, Headers) VALUES (@Body, @Headers) RETURNING QueueID
), m AS (
    INSERT INTO {_metaTable} (QueueID, CorrelationID, QueuedDateTime, Status)
    SELECT b.QueueID, @CorrelationID, now() at time zone 'utc', 0 FROM b
)
SELECT QueueID FROM b;";

            _queueConnection = new QueueConnection(
                "benchPostgreSend" + Guid.NewGuid().ToString("N"), _connectionString);

            _creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(_queueConnection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            _container = new QueueContainer<PostgreSqlMessageQueueInit>();
            _producer = _container.CreateProducer<Event>(_queueConnection);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            //an iteration must not pay for the rows the last one left behind - see the remarks
            Execute($"TRUNCATE TABLE {_metaTable}; TRUNCATE TABLE {_queueTable} RESTART IDENTITY;");
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
                    using var creator = _creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (NpgsqlException)
                {
                    //a queue left behind is not worth failing a run over
                }
            }
            _creation?.Dispose();

            try
            {
                Execute($"DROP TABLE IF EXISTS {_metaTable}; DROP TABLE IF EXISTS {_queueTable};");
            }
            catch (NpgsqlException)
            {
                //same
            }
            NpgsqlConnection.ClearAllPools();
        }

        /// <summary>The floor: a round trip that does no work.</summary>
        [Benchmark(Description = "round trip only: SELECT 1, pooled connection")]
        public int Roundtrip_SelectOne()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>One insert and nothing else - the write, without the transport's shape.</summary>
        [Benchmark(Baseline = true, Description = "raw: 1 insert, pooled connection")]
        public long RawInsert_OneStatement()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"Insert into {_queueTable} (Body, Headers) VALUES (@Body, @Headers) RETURNING QueueID";
            AddBodyParameters(command);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>
        /// What the transport does today: a pooled connection, an explicit transaction, the body
        /// insert followed by <c>lastval()</c>, the meta insert, and a commit. Four round trips.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, 4 round trips")]
        public long RawInsert_DnwqShape()
        {
            using var connection = new NpgsqlConnection(_connectionString);
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
                command.Parameters.Add("@QueueID", NpgsqlDbType.Bigint).Value = id;
                command.Parameters.Add(CorrelationParameter, NpgsqlDbType.Uuid).Value = Guid.NewGuid();
                command.ExecuteNonQuery();
            }

            trans.Commit();
            return id;
        }

        /// <summary>
        /// The identical work as one statement. Against the row above, this is what the extra
        /// three round trips cost.
        /// </summary>
        [Benchmark(Description = "raw: DNWQ shape, 1 round trip (same work)")]
        public long RawInsert_OneRoundTrip()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = _oneRoundTripSql;
            AddBodyParameters(command);
            command.Parameters.Add(CorrelationParameter, NpgsqlDbType.Uuid).Value = Guid.NewGuid();
            return Convert.ToInt64(command.ExecuteScalar());
        }

        /// <summary>The pool, so a connection's cost is on the table rather than assumed.</summary>
        [Benchmark(Description = "pooled connection open + close, no work")]
        public void Connection_OpenClose()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
        }

        /// <summary>
        /// The whole send, as a caller experiences it. Against the four-round-trip raw row, what
        /// is left is the library.
        /// </summary>
        [Benchmark(Description = "DotNetWorkQueue PostgreSQL send (end to end)")]
        public void Transport_Send()
        {
            var result = _producer.Send(new Event { Body = _payload });
            if (result.HasError) throw result.SendingException ?? new InvalidOperationException("send failed");
        }

        private void AddBodyParameters(NpgsqlCommand command)
        {
            command.Parameters.Add(BodyParameter, NpgsqlDbType.Bytea, -1).Value = _body;
            command.Parameters.Add(HeadersParameter, NpgsqlDbType.Bytea, -1).Value = _headers;
        }

        private void Execute(string sql)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public sealed class Event
        {
            public string Body { get; set; }
        }
    }
}
