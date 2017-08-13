using System;
using System.Data;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandSetup;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class SetupCommand : ISetupCommand
    {
        /// <summary>
        /// Setup the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Setup(IDbCommand command, CommandStringTypes type, object commandParams)
        {
            switch (type)
            {
                case CommandStringTypes.ResetHeartbeat:
                    var resetHeartbeatSetup = new ResetHeartbeatSetup();
                    resetHeartbeatSetup.Setup(command, type, commandParams);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
