using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandSetup
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class FindExpiredMessagesToDeleteSetup : ISetupCommand
    {
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindExpiredMessagesToDeleteSetup"/> class.
        /// </summary>
        /// <param name="timeFactory">The time factory.</param>
        public FindExpiredMessagesToDeleteSetup(IGetTimeFactory timeFactory)
        {
            Guard.NotNull(() => timeFactory, timeFactory);
            _getTime = timeFactory.Create();
        }
        /// <summary>
        /// Setup the specified input command.
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        public void Setup(IDbCommand inputCommand, CommandStringTypes type, object commandParams)
        {
            var command = (SQLiteCommand)inputCommand;
            command.Parameters.Add("@CurrentDateTime", DbType.Int64);
            command.Parameters["@CurrentDateTime"].Value = _getTime.GetCurrentUtcDate().Ticks;
        }
    }
}
