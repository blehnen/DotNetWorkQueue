using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IPrepareCommandHandlerWithOutput<in TCommand, out TOutput>
    {
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns></returns>
        TOutput Handle(TCommand command, IDbCommand dbCommand, CommandStringTypes commandType);
    }
}
