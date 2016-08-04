// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Creates a queue and saves the configuration
    /// </summary>
    internal class CreateQueueTablesAndSaveConfigurationCommandHandler : ICommandHandlerWithOutput<CreateQueueTablesAndSaveConfigurationCommand, QueueCreationResult>
    {
        private readonly IInternalSerializer _serializer;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;
        private readonly IConnectionInformation _connectionInformation;
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly ISqLiteTransactionFactory _transactionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateQueueTablesAndSaveConfigurationCommandHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public CreateQueueTablesAndSaveConfigurationCommandHandler(ISqLiteMessageQueueTransportOptionsFactory options, 
            IInternalSerializer serializer, 
            IConnectionInformation connectionInformation,
            SqLiteCommandStringCache commandCache,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => transactionFactory, transactionFactory);

            _serializer = serializer;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
            _connectionInformation = connectionInformation;
            _commandCache = commandCache;
            _transactionFactory = transactionFactory;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        public QueueCreationResult Handle(CreateQueueTablesAndSaveConfigurationCommand command)
        {
            var script = string.Empty;
            try
            {
                if(!DatabaseExists.Exists(_connectionInformation.ConnectionString))
                { //no db file, create
                    var fileName = GetFileNameFromConnectionString.GetFileName(_connectionInformation.ConnectionString);
                    SQLiteConnection.CreateFile(fileName.FileName);
                }
                using (var conn = new SQLiteConnection(_connectionInformation.ConnectionString))
                {
                    conn.Open();
                    using (var trans = _transactionFactory.Create(conn).BeginTransaction())
                    {
                        foreach (var t in command.Tables)
                        {
                            using (var commandSql = conn.CreateCommand())
                            {
                                script = t.Script();
                                commandSql.Transaction = trans;
                                commandSql.CommandText = script;
                                commandSql.ExecuteNonQuery();
                            }
                        }

                        //save the configuration
                        SaveConfiguration(conn, trans);
                        trans.Commit();
                    }
                }

                return new QueueCreationResult(QueueCreationStatus.Success);
            }
            //if the queue already exists, return that status; otherwise, bubble the error
            catch (SQLiteException error)
            {
                if (error.ResultCode == SQLiteErrorCode.Error && error.Message.Contains("table") && error.Message.Contains("already exists"))
                {
                    return new QueueCreationResult(QueueCreationStatus.AttemptedToCreateAlreadyExists);
                }
                throw new DotNetWorkQueueException($"Failed to create queue. SQL script was {script}",
                    error);
            }
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        /// <param name="conn">The connection.</param>
        /// <param name="trans">The trans.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "query is ok")]
        private void SaveConfiguration(SQLiteConnection conn, SQLiteTransaction trans)
        {
            using (var commandSql = conn.CreateCommand())
            {
                commandSql.Transaction = trans;
                commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.SaveConfiguration);
                commandSql.Parameters.Add("@Configuration", DbType.Binary, -1).Value =
                    _serializer.ConvertToBytes(_options.Value);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
