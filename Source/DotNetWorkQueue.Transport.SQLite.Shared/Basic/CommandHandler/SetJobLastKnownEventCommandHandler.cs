// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using System.Data;
using System.Globalization;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.CommandHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand<IDbConnection, IDbTransaction>>
    {
        private readonly IDbCommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public SetJobLastKnownEventCommandHandler(IDbCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(SetJobLastKnownEventCommand<IDbConnection, IDbTransaction> command)
        {
            using (var commandSql = command.Connection.CreateCommand())
            {
                commandSql.Transaction = commandSql.Transaction;
                commandSql.CommandText = _commandCache.GetCommand(CommandStringTypes.SetJobLastKnownEvent);

                var param = commandSql.CreateParameter();
                param.ParameterName = "@JobName";
                param.DbType = DbType.AnsiString;
                param.Value = command.JobName;
                commandSql.Parameters.Add(param);

                param = commandSql.CreateParameter();
                param.ParameterName = "@JobEventTime";
                param.DbType = DbType.AnsiString;
                param.Value = command.JobEventTime.ToString(CultureInfo.InvariantCulture);
                commandSql.Parameters.Add(param);

                param = commandSql.CreateParameter();
                param.ParameterName = "@JobScheduledTime";
                param.DbType = DbType.AnsiString;
                param.Value = command.JobScheduledTime.ToString(CultureInfo.InvariantCulture);
                commandSql.Parameters.Add(param);

                commandSql.ExecuteNonQuery();
            }
        }
    }
}
