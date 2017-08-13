using System;
using System.Data;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandSetup
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
            var sqlCommand = (SqlCommand) inputCommand;
            var commandInput = (ResetHeartBeatCommand)commandParams;

            sqlCommand.Parameters.Add("@QueueID", SqlDbType.BigInt);
            sqlCommand.Parameters.Add("@SourceStatus", SqlDbType.Int);
            sqlCommand.Parameters.Add("@Status", SqlDbType.Int);
            sqlCommand.Parameters.Add("@HeartBeat", SqlDbType.DateTime);
            sqlCommand.Parameters["@QueueID"].Value = commandInput.MessageReset.QueueId;
            sqlCommand.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
            sqlCommand.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
            sqlCommand.Parameters["@HeartBeat"].Value = commandInput.MessageReset.HeartBeat;
        }
    }
}
