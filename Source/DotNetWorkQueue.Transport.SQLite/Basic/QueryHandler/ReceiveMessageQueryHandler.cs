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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Data.SQLite;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal>
    {
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly IConnectionInformation _connectionInformation;
        private readonly MessageDeQueue _messageDeQueue;
        private readonly BuildDequeueCommand _buildDequeueCommand;
        private readonly IDbFactory _dbFactory;
        private readonly DatabaseExists _databaseExists;
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// The dequeue script, built on first use for a given set of routes and caller clause and
        /// kept. Building it measured 541 ns and 6,848 B - 91% of everything a dequeue allocated -
        /// and it is also what identifies the statements a pooled connection has already compiled,
        /// so a stable string matters twice.
        /// </summary>
        private readonly ConcurrentDictionary<string, CommandString> _dequeueScripts =
            new ConcurrentDictionary<string, CommandString>(StringComparer.Ordinal);

        /// <summary>
        /// A process sees one script per consumer in practice. The bound is here because the key
        /// includes the caller's clause, which a caller supplying it through a factory may vary.
        /// </summary>
        private const int MaxCachedScripts = 32;

        /// <summary>Guards admission to <see cref="_dequeueScripts"/> so the cap cannot be exceeded.</summary>
        private readonly object Admission = new object();

        /// <summary>Initializes a new instance of the <see cref="ReceiveMessageQueryHandler" /> class.</summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="buildDequeueCommand">The build dequeue command.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        /// <param name="dbFactory">The transaction factory.</param>
        /// <param name="databaseExists">The database exists.</param>
        /// <param name="configuration">Queue configuration</param>
        public ReceiveMessageQueryHandler(ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            ITableNameHelper tableNameHelper,
            IConnectionInformation connectionInformation,
            BuildDequeueCommand buildDequeueCommand,
            MessageDeQueue messageDeQueue,
            IDbFactory dbFactory,
            DatabaseExists databaseExists,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(optionsFactory);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(buildDequeueCommand);
            Guard.NotNull(messageDeQueue);
            Guard.NotNull(databaseExists);
            Guard.NotNull(dbFactory);
            Guard.NotNull(configuration);

            _options = new Lazy<SqLiteMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _buildDequeueCommand = buildDequeueCommand;
            _messageDeQueue = messageDeQueue;
            _dbFactory = dbFactory;
            _databaseExists = databaseExists;
            _configuration = configuration;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery<IDbConnection, IDbTransaction> query)
        {
            if (!_databaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return null;
            }

            //The clause and the parameters are read on every dequeue, because either may come from
            //a caller-supplied factory that is meant to be consulted every time. Only the script is
            //cached, and the clause is part of its key - the parameters are bound rather than
            //written into the SQL, so their values cannot affect it.
            var userClause = _options.Value.AdditionalColumnsOnMetaData ? _configuration.GetUserClause() : null;
            List<SQLiteParameter> userParameters = null;
            if (_options.Value.AdditionalColumnsOnMetaData && !string.IsNullOrEmpty(userClause))
                userParameters = _configuration.GetUserParameters(); //NOTE - could be null

            var commandString = GetDeQueueCommand(userClause, query.Routes);

            using (var connection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false))
            {
                connection.Open();
                using (var transaction = _dbFactory.CreateTransaction(connection).BeginTransaction())
                {
                    //asking the factory for the command, rather than the connection, is what lets a
                    //pooled connection hand back the statements it already compiled for this text
                    using (var selectCommand = _dbFactory.CreateCommand(connection, commandString.CommandText))
                    {
                        selectCommand.Transaction = transaction;

                        _buildDequeueCommand.BuildCommand(selectCommand, commandString, _options.Value,
                            query.Routes, userParameters);
                        using (var reader = selectCommand.ExecuteReader())
                        {
                            return _messageDeQueue.HandleMessage(connection, transaction, reader, commandString);
                        }
                    }
                }
            }
        }

        /// <summary>The dequeue script for a set of routes and a caller clause, built once and kept.</summary>
        /// <param name="userClause">The caller's additional where clause, if any.</param>
        /// <param name="routes">The routes.</param>
        private CommandString GetDeQueueCommand(string userClause, List<string> routes)
        {
            var key = Key(userClause, routes);

            if (_dequeueScripts.TryGetValue(key, out var cached))
                return cached;

            var built = ReceiveMessage.GetDeQueueCommand(_tableNameHelper.MetaDataName, _tableNameHelper.QueueName,
                _tableNameHelper.StatusName, _options.Value, userClause, routes);

            //Admission is serialised so the cap is an actual bound; there is no eviction, so an
            //overshoot would be permanent. This runs only on a miss, which after warm-up is never.
            lock (Admission)
            {
                if (_dequeueScripts.Count < MaxCachedScripts)
                    _dequeueScripts.TryAdd(key, built);
            }

            return built;
        }

        /// <summary>
        /// Everything the script depends on that is not fixed for the life of this handler.
        /// </summary>
        /// <remarks>
        /// Length-prefixed rather than separator-joined. A separator can be split or joined by a
        /// clause or route value that contains it, and two different combinations reaching the same
        /// key would matter: the number of routes decides how many placeholders the SQL carries, so
        /// a collision could hand back a script of the wrong shape.
        /// </remarks>
        internal static string Key(string userClause, List<string> routes)
        {
            var hasClause = !string.IsNullOrEmpty(userClause);
            var hasRoutes = routes != null && routes.Count > 0;

            //the ordinary case allocates nothing
            if (!hasClause && !hasRoutes)
                return string.Empty;

            var key = new StringBuilder();
            key.Append(hasClause ? userClause.Length : 0).Append(':').Append(userClause);
            key.Append('|').Append(hasRoutes ? routes.Count : 0);

            if (hasRoutes)
            {
                foreach (var route in routes)
                    key.Append(':').Append(route?.Length ?? 0).Append(':').Append(route);
            }

            return key.ToString();
        }
    }
}
