using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandSetup
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
            var command = (SQLiteCommand) inputCommand;
            var commandInput = (ResetHeartBeatCommand)commandParams;

            command.Parameters.Add("@QueueID", DbType.Int64);
            command.Parameters.Add("@SourceStatus", DbType.Int32);
            command.Parameters.Add("@Status", DbType.Int32);
            command.Parameters.Add("@HeartBeat", DbType.DateTime2);
            command.Parameters["@QueueID"].Value = commandInput.MessageReset.QueueId;
            command.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Waiting);
            command.Parameters["@SourceStatus"].Value = Convert.ToInt16(QueueStatuses.Processing);
            command.Parameters["@HeartBeat"].Value = commandInput.MessageReset.HeartBeat.Ticks;
        }
    }
}
