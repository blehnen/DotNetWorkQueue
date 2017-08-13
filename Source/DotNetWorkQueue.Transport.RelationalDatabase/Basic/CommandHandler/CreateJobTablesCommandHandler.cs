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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <summary>
    /// Creates tables for storing job info
    /// </summary>
    public class CreateJobTablesCommandHandler : ICommandHandlerWithOutput<CreateJobTablesCommand<ITable>, QueueCreationResult>
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateJobTablesCommandHandler" /> class.
        /// </summary>
        /// <param name="dbConnectionFactory">The connection factory.</param>
        public CreateJobTablesCommandHandler(IDbConnectionFactory dbConnectionFactory)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public QueueCreationResult Handle(CreateJobTablesCommand<ITable> command)
        {
            using (var conn = _dbConnectionFactory.Create())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    foreach (var t in command.Tables)
                    {
                        using (var commandSql = conn.CreateCommand())
                        {
                            commandSql.Transaction = trans;
                            commandSql.CommandText = t.Script();
                            commandSql.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
            }
            return new QueueCreationResult(QueueCreationStatus.Success);
        }
    }
}
