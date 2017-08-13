using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandSetup
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class ResetHeartbeatSetup : ISetupCommand
    {
        /// <summary>
        /// Setup the specified input command.
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        public void Setup(IDbCommand inputCommand, CommandStringTypes type, object commandParams)
        {
            var npgsqlCommand = (NpgsqlCommand) inputCommand;
            var commandInput = (ResetHeartBeatCommand)commandParams;

            npgsqlCommand.Parameters.Add("@QueueID", NpgsqlDbType.Bigint);
            npgsqlCommand.Parameters.Add("@SourceStatus", NpgsqlDbType.Integer);
            npgsqlCommand.Parameters.Add("@Status", NpgsqlDbType.Integer);
            npgsqlCommand.Parameters.Add("@HeartBeat", NpgsqlDbType.Bigint);
            npgsqlCommand.Parameters["@QueueID"].Value = commandInput.MessageReset.QueueId;
            npgsqlCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
            npgsqlCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
            npgsqlCommand.Parameters["@HeartBeat"].Value = commandInput.MessageReset.HeartBeat.Ticks;
        }
    }
}
