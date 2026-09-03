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
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Npgsql;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// The PostgreSQL de-queue statement, measured before assuming the SQL Server result transfers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ReceiveMessage.GetDeQueueCommand</c> carried the same deferral note SQL Server's did, and
    /// the same defect: the cached statement was returned only when there were no routes and no
    /// user clause, so a consumer using either reassembled it on every poll.
    /// </para>
    /// <para>
    /// The number did not transfer, which is why it was measured rather than assumed. PostgreSQL
    /// uses an updating CTE with <c>FOR UPDATE SKIP LOCKED</c> rather than a table variable, so its
    /// statement is smaller: <b>2,648 B a poll against SQL Server's 5,368 B</b>. Real either way,
    /// and about half the size.
    /// </para>
    /// <para>
    /// Both shapes are cached now, keyed on the route count and the clause - 80 B, a 97% cut. These
    /// rungs therefore measure cache hits, and are kept as the regression guard: if that keying
    /// stops working, the routed row goes back to kilobytes a poll and says so.
    /// </para>
    /// <para>
    /// <b>Requires a PostgreSQL instance</b> via <c>DNWQ_POSTGRES_CONNECTION</c>.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class PostgreSqlReceiveBenchmarks
    {
        private string _connectionString;
        private QueueConnection _queueConnection;
        private QueueCreationContainer<PostgreSqlMessageQueueInit> _creation;
        private QueueContainer<PostgreSqlMessageQueueInit> _container;
        private IConsumerQueue _consumer;

        private PostgreSqlCommandStringCache _commandCache;
        private ITableNameHelper _tableNameHelper;
        private PostgreSqlMessageQueueTransportOptions _options;
        private QueueConsumerConfiguration _configuration;

        private List<string> _routes;

        [GlobalSetup]
        public void Setup()
        {
            _connectionString = Environment.GetEnvironmentVariable("DNWQ_POSTGRES_CONNECTION");
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_POSTGRES_CONNECTION to a PostgreSQL connection string.");

            _routes = new List<string> { "a-route" };

            _queueConnection = new QueueConnection(
                "benchpgrecv" + Guid.NewGuid().ToString("N"), _connectionString);

            _creation = new QueueCreationContainer<PostgreSqlMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(_queueConnection))
            {
                creator.Options.EnableRoute = true;
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            _container = new QueueContainer<PostgreSqlMessageQueueInit>();
            _consumer = _container.CreateConsumer(_queueConnection);

            var container = ConsumerInternals.ContainerOf(_container);
            _commandCache = container.GetInstance<PostgreSqlCommandStringCache>();
            _tableNameHelper = container.GetInstance<ITableNameHelper>();
            _options = container.GetInstance<IPostgreSqlMessageQueueTransportOptionsFactory>().Create();
            _configuration = container.GetInstance<QueueConsumerConfiguration>();

            //prime both keys so each rung measures what it says it measures
            ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options, _configuration, null, out _);
            ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options, _configuration, _routes, out _);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _consumer?.Dispose();
            _container?.Dispose();
            if (_queueConnection != null)
            {
                try
                {
                    using var creator = _creation.GetQueueCreation<PostgreSqlMessageQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (PostgresException)
                {
                    //a queue left behind is not worth failing a run over
                }
            }
            _creation?.Dispose();
            NpgsqlConnection.ClearAllPools();
        }

        /// <summary>The common case: no routes, so the statement comes from the cache.</summary>
        [Benchmark(Baseline = true, Description = "statement: cached (no routes), no round trip")]
        public int Statement_Cached()
        {
            return ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options,
                _configuration, null, out _).Length;
        }

        /// <summary>
        /// What a routed consumer pays per poll. This rebuilt the statement before routes were
        /// cached - 240 ns and 2,648 B - and is a cache hit now, the 80 B being the composite key.
        /// </summary>
        [Benchmark(Description = "statement: routed consumer, no round trip")]
        public int Statement_Routed()
        {
            return ReceiveMessage.GetDeQueueCommand(_commandCache, _tableNameHelper, _options,
                _configuration, _routes, out _).Length;
        }
    }
}
