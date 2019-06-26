// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand, DateTime?>
    {
        private readonly IPrepareCommandHandlerWithOutput<SendHeartBeatCommand, DateTime> _prepareCommand;
        private readonly IDbConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        /// <param name="prepareCommand">The prepare command.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        public SendHeartBeatCommandHandler(
            IPrepareCommandHandlerWithOutput<SendHeartBeatCommand, DateTime> prepareCommand,
            IDbConnectionFactory connectionFactory)
        {

            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => prepareCommand, prepareCommand);

            _prepareCommand = prepareCommand;
            _connectionFactory = connectionFactory;
        }
        /// <inheritdoc />
        public DateTime? Handle(SendHeartBeatCommand command)
        {
            using (var conn = _connectionFactory.Create())
            {
                conn.Open();
                using (var commandSql = conn.CreateCommand())
                {
                    var date = _prepareCommand.Handle(command, commandSql, CommandStringTypes.SendHeartBeat);
                    var records = commandSql.ExecuteNonQuery();
                    if (records != 1) return null;
                    return date;
                }
            }
        }
    }
}
