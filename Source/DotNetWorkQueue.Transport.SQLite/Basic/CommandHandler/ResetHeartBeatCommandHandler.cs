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
using DotNetWorkQueue.Transport.SQLite.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Resets the status for a specific record
    /// </summary>
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand, long>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        private readonly ISqLiteTransactionFactory _transactionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="transactionFactory">The transaction factory.</param>
        public ResetHeartBeatCommandHandler(SqLiteCommandStringCache commandCache, 
            IConnectionInformation connectionInformation,
            ISqLiteTransactionFactory transactionFactory)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
            _transactionFactory = transactionFactory;
        }
        /// <summary>
        /// Resets the status for a specific record, if the status is currently 1
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        public long Handle(ResetHeartBeatCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return 0;
            }

            using (var connection = new SQLiteConnection(_connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var trans = _transactionFactory.Create(connection).BeginTransaction())
                {
                    using (var sqLiteCommand = connection.CreateCommand())
                    {
                        sqLiteCommand.Transaction = trans;
                        sqLiteCommand.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.ResetHeartbeat);
                        sqLiteCommand.Parameters.Add("@QueueID", DbType.Int64);
                        sqLiteCommand.Parameters.Add("@SourceStatus", DbType.Int32);
                        sqLiteCommand.Parameters.Add("@Status", DbType.Int32);
                        sqLiteCommand.Parameters.Add("@HeartBeat", DbType.DateTime2);
                        sqLiteCommand.Parameters["@QueueID"].Value = command.MessageReset.QueueId;
                        sqLiteCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
                        sqLiteCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
                        sqLiteCommand.Parameters["@HeartBeat"].Value = command.MessageReset.HeartBeat.Ticks;
                        var result = sqLiteCommand.ExecuteNonQuery();
                        if (result > 0)
                        {
                            trans.Commit();
                        }
                        return result;
                    }
                }
            }
        }
    }
}
