using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand<SqlConnection, SqlTransaction>>
    {
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly IConnectionInformation _connectionInformation;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public SetJobLastKnownEventCommandHandler(SqlServerCommandStringCache commandCache,
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
        public void Handle(SetJobLastKnownEventCommand<SqlConnection, SqlTransaction> command)
        {
            using (var conn = new SqlConnection(_connectionInformation.ConnectionString))
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);
                    commandSql.Parameters.Add("@JobName", SqlDbType.VarChar);
                    commandSql.Parameters["@JobName"].Value = command.JobName;
                    commandSql.Parameters.Add("@JobEventTime", SqlDbType.DateTimeOffset);
                    commandSql.Parameters["@JobEventTime"].Value = command.JobEventTime;
                    commandSql.Parameters.Add("@JobScheduledTime", SqlDbType.DateTimeOffset);
                    commandSql.Parameters["@JobScheduledTime"].Value = command.JobScheduledTime;
                    commandSql.ExecuteNonQuery();
                }
            }
        }
    }
}
