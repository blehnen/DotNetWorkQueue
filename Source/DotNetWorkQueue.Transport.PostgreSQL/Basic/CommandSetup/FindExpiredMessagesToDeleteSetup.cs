using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandSetup
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
            var command = (NpgsqlCommand)inputCommand;
            command.Parameters.Add("@CurrentDate", NpgsqlDbType.Bigint);
            command.Parameters["@CurrentDate"].Value = _getTime.GetCurrentUtcDate().Ticks;
        }
    }
}
