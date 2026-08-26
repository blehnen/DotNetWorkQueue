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
using System.Data;
using System.Data.SQLite;
using System.IO;
using BenchmarkDotNet.Attributes;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Decomposes the cost of a single SQLite dequeue, the way <see cref="SendPathBenchmarks"/>
    /// does for a send.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every rung runs against an <em>empty</em> queue. That is deliberate: a dequeue consumes the
    /// row it finds, so measuring against a populated queue would make each iteration depend on
    /// what the previous one left behind, which BenchmarkDotNet cannot control. An empty poll runs
    /// the whole script - the temp table, the candidate select, the join - and simply finds nothing,
    /// so it measures everything except materialising a row and the follow-up status commands. It
    /// is also a real workload in its own right: an idle consumer does exactly this on every poll.
    /// </para>
    /// <para>
    /// <c>SQLiteCommand.Prepare()</c> is a no-op in System.Data.SQLite - statements are compiled
    /// lazily on execution - so preparation cannot be timed on its own. The fresh-versus-reused
    /// pair below measures it where it actually happens.
    /// </para>
    /// </remarks>
    /// <summary>
    /// <see cref="DbFactory"/> resolves a transaction wrapper from the container. These rungs begin
    /// their transactions on the connection directly, so nothing is ever resolved.
    /// </summary>
    internal sealed class NoContainerFactory : IContainerFactory
    {
        public IContainer Create() => null;
    }

    [MemoryDiagnoser]
    public class ReceivePathBenchmarks
    {
        private const string Sync = "NORMAL";
        private const string CurrentDateTimeParam = "@CurrentDateTime";

        private string _dir;
        private string _connectionString;

        private QueueCreationContainer<SqLiteMessageQueueInit> _creation;
        private QueueContainer<SqLiteMessageQueueInit> _container;
        private IConsumerQueue _consumer;

        private TableNameHelper _tableNames;
        private SqLiteMessageQueueTransportOptions _options;
        private QueueConsumerConfiguration _configuration;

        private SQLiteConnection _connection;
        private IDbFactory _dbFactory;
        private SQLiteCommand _reusedCommand;
        private string _cachedSql;

        [GlobalSetup]
        public void Setup()
        {
            _dir = Path.Combine(Path.GetTempPath(), "dnwq-bench-receive", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);

            var path = Path.Combine(_dir, "receive.db");
            _connectionString = $"Data Source={path};Version=3;Synchronous={Sync};";
            var queueConnection = new QueueConnection("benchReceive", _connectionString);

            _creation = new QueueCreationContainer<SqLiteMessageQueueInit>();
            using (var creator = _creation.GetQueueCreation<SqLiteMessageQueueCreation>(queueConnection))
            {
                var result = creator.CreateQueue();
                if (!result.Success)
                    throw new InvalidOperationException($"CreateQueue failed: {result.Status} {result.ErrorMessage}");
            }

            _container = new QueueContainer<SqLiteMessageQueueInit>();
            _consumer = _container.CreateConsumer(queueConnection);

            //the live consumer configuration, so the generated SQL matches what a consumer runs
            _configuration = _consumer.Configuration;

            //the queue above was created with default options, so these are the options it has
            _options = new SqLiteMessageQueueTransportOptions();
            _tableNames = new TableNameHelper(
                new SqliteConnectionInformation(queueConnection, new DbDataSource()));

            _connection = new SQLiteConnection(_connectionString);
            _connection.Open();

            //the transport's own factory, so one rung measures the plumbing that actually ships
            _dbFactory = new DbFactory(new NoContainerFactory());

            //the handler caches the script; generating it per dequeue is the old shape
            _cachedSql = GenerateSql().CommandText;

            _reusedCommand = _connection.CreateCommand();
            _reusedCommand.CommandText = _cachedSql;
            _reusedCommand.Parameters.Add(CurrentDateTimeParam, DbType.Int64).Value = DateTime.UtcNow.Ticks;

            //the first execution compiles the statements; every rung should measure a warm database
            _reusedCommand.ExecuteNonQuery();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            (_dbFactory as IDisposable)?.Dispose();
            _reusedCommand?.Dispose();
            _connection?.Dispose();
            _consumer?.Dispose();
            _container?.Dispose();
            _creation?.Dispose();
            SQLiteConnection.ClearAllPools();
            try
            {
                Directory.Delete(_dir, true);
            }
            catch (IOException)
            {
                //a scratch directory that outlives the run is harmless
            }
            catch (UnauthorizedAccessException)
            {
                //a scratch directory that outlives the run is harmless
            }
        }

        private CommandString GenerateSql() =>
            ReceiveMessage.GetDeQueueCommand(_tableNames.MetaDataName, _tableNames.QueueName,
                _tableNames.StatusName, _options, null, null);

        /// <summary>
        /// Building the dequeue script. The transport does this on every dequeue, unlike the body
        /// insert on the send path, which is served from <c>IDbCommandStringCache</c>.
        /// </summary>
        [Benchmark(Description = "generate the dequeue SQL")]
        public int GenerateDequeueSql() => GenerateSql().CommandText.Length;

        /// <summary>
        /// A dequeue as the transport performs it today: the script generated afresh, onto a command
        /// created afresh, so SQLite compiles every statement again.
        /// </summary>
        [Benchmark(Baseline = true, Description = "dequeue an empty queue, fresh command (as today)")]
        public object Dequeue_FreshCommand()
        {
            using var command = _connection.CreateCommand();
            command.CommandText = GenerateSql().CommandText;
            command.Parameters.Add(CurrentDateTimeParam, DbType.Int64).Value = DateTime.UtcNow.Ticks;
            using var reader = command.ExecuteReader();
            return reader.Read() ? reader[0] : null;
        }

        /// <summary>
        /// The same dequeue acquired the way the transport acquires it: a pooled connection from
        /// <see cref="DbFactory"/> and a command asked of the factory rather than of the connection.
        /// Measured against <see cref="Dequeue_FreshCommand"/>, which acquires its command the old
        /// way and is otherwise identical, this says whether the reuse is actually being delivered.
        /// </summary>
        /// <remarks>
        /// This is not a whole consumer dequeue. <c>ReceiveMessageQueryHandler.Handle</c> also opens
        /// a transaction and goes through <c>BuildDequeueCommand</c>, neither of which is here, so
        /// the number is command acquisition and execution only. Both rungs omit the same things,
        /// so the ratio between them is the meaningful part.
        /// </remarks>
        [Benchmark(Description = "dequeue an empty queue, command from the factory cache")]
        public object Dequeue_ThroughTransportPlumbing()
        {
            using var connection = _dbFactory.CreateConnection(_connectionString, false);
            connection.Open();
            using var command = _dbFactory.CreateCommand(connection, GenerateSql().CommandText);

            var parameter = command.CreateParameter();
            parameter.ParameterName = CurrentDateTimeParam;
            parameter.DbType = DbType.Int64;
            parameter.Value = DateTime.UtcNow.Ticks;
            command.Parameters.Add(parameter);

            using var reader = command.ExecuteReader();
            return reader.Read() ? reader[0] : null;
        }

        /// <summary>
        /// What the transport does now: the script cached rather than rebuilt, and the command
        /// asked of the factory so its compiled statements come back too. Against the rung above,
        /// which rebuilds the script, this is what caching the script is worth.
        /// </summary>
        [Benchmark(Description = "dequeue an empty queue, script and command both cached")]
        public object Dequeue_ScriptAndCommandCached()
        {
            using var connection = _dbFactory.CreateConnection(_connectionString, false);
            connection.Open();
            using var command = _dbFactory.CreateCommand(connection, _cachedSql);

            var parameter = command.CreateParameter();
            parameter.ParameterName = CurrentDateTimeParam;
            parameter.DbType = DbType.Int64;
            parameter.Value = DateTime.UtcNow.Ticks;
            command.Parameters.Add(parameter);

            using var reader = command.ExecuteReader();
            return reader.Read() ? reader[0] : null;
        }

        /// <summary>
        /// The reuse a transport could realistically ship. Callers add their own parameters through
        /// <c>CreateParameter</c>, so a cached command has to hand back an empty parameter
        /// collection each time; only the compiled statement is kept. If this lands near the rung
        /// below rather than the one above, keeping the command and rebuilding the parameters is
        /// enough, and callers do not have to change.
        /// </summary>
        [Benchmark(Description = "dequeue an empty queue, command reused, parameters rebuilt")]
        public object Dequeue_ReusedCommand_ParametersRebuilt()
        {
            _reusedCommand.Parameters.Clear();
            var parameter = _reusedCommand.CreateParameter();
            parameter.ParameterName = CurrentDateTimeParam;
            parameter.DbType = DbType.Int64;
            parameter.Value = DateTime.UtcNow.Ticks;
            _reusedCommand.Parameters.Add(parameter);

            using var reader = _reusedCommand.ExecuteReader();
            return reader.Read() ? reader[0] : null;
        }

        /// <summary>
        /// The same dequeue with the command built once and only its parameter rebound. Minus the
        /// row above, this is what generating the script and recompiling its statements costs on
        /// every dequeue.
        /// </summary>
        [Benchmark(Description = "dequeue an empty queue, command reused")]
        public object Dequeue_ReusedCommand()
        {
            _reusedCommand.Parameters[CurrentDateTimeParam].Value = DateTime.UtcNow.Ticks;
            using var reader = _reusedCommand.ExecuteReader();
            return reader.Read() ? reader[0] : null;
        }
    }
}
