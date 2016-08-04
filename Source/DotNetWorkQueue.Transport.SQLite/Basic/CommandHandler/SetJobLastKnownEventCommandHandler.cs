// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Transport.SQLite.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand>
    {
        private readonly SqLiteCommandStringCache _commandCache;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler" /> class.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        public SetJobLastKnownEventCommandHandler(SqLiteCommandStringCache commandCache)
        {
            Guard.NotNull(() => commandCache, commandCache);
            _commandCache = commandCache;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(SetJobLastKnownEventCommand command)
        {
            using (var commandSql = command.Connection.CreateCommand())
            {
                commandSql.Transaction = commandSql.Transaction;
                commandSql.CommandText = _commandCache.GetCommand(SqLiteCommandStringTypes.SetJobLastKnownEvent);
                commandSql.Parameters.Add("@JobName", DbType.String);
                commandSql.Parameters["@JobName"].Value = command.JobName;
                commandSql.Parameters.Add("@JobEventTime", DbType.String);
                commandSql.Parameters["@JobEventTime"].Value =
                    command.JobEventTime.ToString(System.Globalization.CultureInfo.InvariantCulture);
                commandSql.Parameters.Add("@JobScheduledTime", DbType.String);
                commandSql.Parameters["@JobScheduledTime"].Value =
                    command.JobScheduledTime.ToString(System.Globalization.CultureInfo.InvariantCulture);
                commandSql.ExecuteNonQuery();
            }
        }
    }
}
