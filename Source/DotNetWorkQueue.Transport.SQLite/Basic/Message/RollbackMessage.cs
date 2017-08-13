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
using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Message
{
    /// <summary>
    /// Rolls back a message by either rolling back a transaction or updating a status
    /// </summary>
    internal class RollbackMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<RollbackMessageCommand> _rollbackCommand;
        private readonly SqlHeaders _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessage"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="rollbackCommand">The rollback command.</param>
        /// <param name="headers">The headers.</param>
        public RollbackMessage(QueueConsumerConfiguration configuration,
            ICommandHandler<RollbackMessageCommand> rollbackCommand,
            SqlHeaders headers)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => rollbackCommand, rollbackCommand);
            Guard.NotNull(() => headers, headers);

            _configuration = configuration;
            _rollbackCommand = rollbackCommand;
            _headers = headers;
        }
        /// <summary>
        /// Rollbacks the specified message by setting the status
        /// </summary>
        /// <param name="context">The context.</param>
        public void Rollback(IMessageContext context)
        {
            if (context.MessageId == null || !context.MessageId.HasValue) return;

            //there is nothing to rollback unless at least one of these options is enabled
            if (_configuration.Options().EnableDelayedProcessing ||
                _configuration.Options().EnableHeartBeat ||
                _configuration.Options().EnableStatus)
            {
                DateTime? lastHeartBeat = null;
                if (context.WorkerNotification?.HeartBeat?.Status?.LastHeartBeatTime != null)
                {
                    lastHeartBeat = context.WorkerNotification.HeartBeat.Status.LastHeartBeatTime.Value;
                }

                var increaseDelay = context.Get(_headers.IncreaseQueueDelay).IncreaseDelay;
                _rollbackCommand.Handle(new RollbackMessageCommand(lastHeartBeat,
                    (long)context.MessageId.Id.Value, increaseDelay));
            }
        }
    }
}
