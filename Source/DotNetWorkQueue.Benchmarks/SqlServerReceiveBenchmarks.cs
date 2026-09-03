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
using DotNetWorkQueue.Transport.SqlServer.Basic;
using DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler;
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// The de-queue script, which is the question #231 inherited from the SQLite pass - there,
    /// generating it was 91% of everything a de-queue allocated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On SQL Server it is already cached, so the SQLite finding does not transfer. But the cache
    /// is conditional: <c>CreateDequeueStatement.GetDeQueueCommand</c> returns the cached text only
    /// when there are no routes and no user clause. A consumer using either rebuilds the whole
    /// statement - 43 <c>StringBuilder</c> appends, a table variable and a CTE - on every poll.
    /// That is what these rungs measure.
    /// </para>
    /// <para>
    /// The de-queue rungs run against an <b>empty</b> queue. A de-queue consumes the row it finds,
    /// so a populated queue would make each iteration depend on what the last one left behind,
    /// which BenchmarkDotNet cannot control. An empty poll still runs the whole statement and
    /// simply finds nothing - and it is a real workload, since an idle consumer does exactly this.
    /// </para>
    /// <para>
    /// <b>Requires a SQL Server instance</b> via <c>DNWQ_SQLSERVER_CONNECTION</c>, as
    /// <see cref="SqlServerPathBenchmarks"/> does.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    public class SqlServerReceiveBenchmarks
    {
        private string _connectionString;
        private QueueConnection _queueConnection;
        private QueueCreationContainer<SqlServerMessageQueueInit> _creation;
        private QueueContainer<SqlServerMessageQueueInit> _container;
        private IConsumerQueue _consumer;

        private CreateDequeueStatement _statement;
        private IReceiveMessages _receive;
        private IMessageContextFactory _contextFactory;

        private List<string> _routes;

        [GlobalSetup]
        public void Setup()
        {
            _connectionString = Environment.GetEnvironmentVariable("DNWQ_SQLSERVER_CONNECTION");
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException(
                    "Set DNWQ_SQLSERVER_CONNECTION to a SQL Server connection string.");

            _routes = new List<string> { "a-route" };

            _queueConnection = new QueueConnection(
                "benchSqlServerRecv" + Guid.NewGuid().ToString("N"), _connectionString);

            _creation = new QueueCreationContainer<SqlServerMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<SqlServerMessageQueueCreation>(_queueConnection))
            {
                //routes on, so the rebuilt-statement path is available to measure
                creator.Options.EnableRoute = true;
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            _container = new QueueContainer<SqlServerMessageQueueInit>();
            _consumer = _container.CreateConsumer(_queueConnection);

            var container = ConsumerInternals.ContainerOf(_container);
            _statement = container.GetInstance<CreateDequeueStatement>();
            _contextFactory = container.GetInstance<IMessageContextFactory>();
            _receive = container.GetInstance<IReceiveMessagesFactory>().Create();

            //prime the cache so the cached rung measures a hit rather than the first build
            _statement.GetDeQueueCommand(out _);
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
                    using var creator = _creation.GetQueueCreation<SqlServerMessageQueueCreation>(_queueConnection);
                    creator.RemoveQueue();
                }
                catch (SqlException)
                {
                    //a queue left behind is not worth failing a run over
                }
            }
            _creation?.Dispose();
            SqlConnection.ClearAllPools();
        }

        /// <summary>The common case: no routes, so the statement comes from the cache.</summary>
        [Benchmark(Baseline = true, Description = "statement: cached (no routes), no round trip")]
        public int Statement_Cached()
        {
            return _statement.GetDeQueueCommand(out _).Length;
        }

        /// <summary>
        /// What a routed consumer pays per poll.
        /// </summary>
        /// <remarks>
        /// This rung changed meaning with the fix it was written to justify, which is worth being
        /// explicit about. Before routes were cached it rebuilt the statement on every call - 439 ns
        /// and 5,368 B - because routes bypassed the cache entirely. It now measures a cache hit for
        /// the routed shape, at 41 ns and 80 B, the 80 B being the composite key.
        /// <para>
        /// It is kept rather than deleted because it is the regression guard: if the route cache
        /// ever stops working, this row goes back to allocating kilobytes per poll and says so. A
        /// rung that forces a genuine rebuild is not reachable through this API without inventing a
        /// new route count on every call, which would grow the cache without bound - the thing the
        /// key is designed to avoid.
        /// </para>
        /// </remarks>
        [Benchmark(Description = "statement: routed consumer, no round trip")]
        public int Statement_Routed()
        {
            return _statement.GetDeQueueCommand(out _, _routes).Length;
        }

        /// <summary>A poll against an empty queue, no routes - the whole statement, one round trip.</summary>
        [Benchmark(Description = "de-queue, empty queue, no routes (end to end)")]
        public void Dequeue_NoRoutes()
        {
            using var context = _contextFactory.Create();
            _receive.ReceiveMessage(context);
        }
    }
}
