// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler
{
    /// <inheritdoc />
    public class MoveRecordToErrorQueueCommandPrepareHandler: IPrepareCommandHandler<MoveRecordToErrorQueueCommand<long>>
    {
        private readonly IBuildMoveToErrorQueueSql _buildSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveRecordToErrorQueueCommandPrepareHandler"/> class.
        /// </summary>
        /// <param name="buildSql">The build SQL.</param>
        public MoveRecordToErrorQueueCommandPrepareHandler(IBuildMoveToErrorQueueSql buildSql)
        {
            Guard.NotNull(() => buildSql, buildSql);
            _buildSql = buildSql;
        }
        /// <inheritdoc />
        public void Handle(MoveRecordToErrorQueueCommand<long> command, IDbCommand dbCommand, CommandStringTypes commandType)
        {
            dbCommand.CommandText = _buildSql.Create();

            var queueId = dbCommand.CreateParameter();
            queueId.ParameterName = "@QueueID";
            queueId.DbType = DbType.Int64;
            queueId.Value = command.QueueId;
            dbCommand.Parameters.Add(queueId);

            var exception = dbCommand.CreateParameter();
            exception.ParameterName = "@LastException";
            exception.DbType = DbType.AnsiString;
            exception.Value = command.Exception.ToString();
            dbCommand.Parameters.Add(exception);
        }
    }
}
