using System;
using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SendHeartBeatCommandHandler(SqlServerCommandStringCache commandCache, 
            IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => commandCache, commandCache);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _commandCache = commandCache;
            _connectionInformation = connectionInformation;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public DateTime? Handle(SendHeartBeatCommand command)
        {
            using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SendHeartBeat);
                    commandSql.Parameters.Add("@QueueID", SqlDbType.BigInt);
                    commandSql.Parameters["@QueueID"].Value = command.QueueId;
                    commandSql.Parameters.Add("@status", SqlDbType.Int);
                    commandSql.Parameters["@status"].Value = Convert.ToInt16(QueueStatuses.Processing);
                    using (var reader = commandSql.ExecuteReader())
                    {
                        if (reader.RecordsAffected != 1) return null; //return null if the record was not updated.
                        if (reader.Read())
                        {
                            return reader.GetDateTime(0);
                        }
                    }
                    return null; //return null if the record was not updated.
                }
            }
        }
    }
}
