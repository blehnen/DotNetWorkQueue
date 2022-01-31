// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandPrepareHandler
{
    /// <summary>
    /// 
    /// </summary>
    public class MoveRecordToErrorQueueCommandPrepareHandler : IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>>
    {
        private readonly IBuildMoveToErrorQueueSql _buildSql;
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandPrepareHandler" /> class.
        /// </summary>
        /// <param name="buildSql">The build SQL.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        public MoveRecordToErrorQueueCommandPrepareHandler(IBuildMoveToErrorQueueSql buildSql,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(() => buildSql, buildSql);
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            _buildSql = buildSql;
            _getTime = getTimeFactory.Create();
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dbCommand">The database command.</param>
        /// <param name="commandType">Type of the command.</param>
        public void Handle(MoveRecordToErrorQueueCommand<long> command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _buildSql.Create();
            var commandSql = dbCommand;

            var param = commandSql.CreateParameter();
            param.ParameterName = "@QueueID";
            param.DbType = DbType.Int64;
            param.Value = command.QueueId;
            commandSql.Parameters.Add(param);

            param = commandSql.CreateParameter();
            param.ParameterName = "@LastException";
            param.DbType = DbType.String;
            param.Value = command.Exception.ToString();
            commandSql.Parameters.Add(param);

            param = commandSql.CreateParameter();
            param.ParameterName = "@CurrentDateTime";
            param.DbType = DbType.Int64;
            param.Value = _getTime.GetCurrentUtcDate().Ticks;
            commandSql.Parameters.Add(param);
        }
    }
}
