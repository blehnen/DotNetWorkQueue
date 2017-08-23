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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Message
{
    /// <summary>
    /// Rolls back a message by either rolling back a transaction or updating a status
    /// </summary>
    internal class RollbackMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<RollbackMessageCommand> _rollbackCommand;
        private readonly ICommandHandler<SetStatusTableStatusCommand> _setStatusCommandHandler;
        private readonly SqlHeaders _headers;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="rollbackCommand">The rollback command.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="log">The log.</param>
        public RollbackMessage(QueueConsumerConfiguration configuration,
            ICommandHandler<RollbackMessageCommand> rollbackCommand,
            ICommandHandler<SetStatusTableStatusCommand> setStatusCommandHandler,
            SqlHeaders headers,
            ILogFactory log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => rollbackCommand, rollbackCommand);
            Guard.NotNull(() => setStatusCommandHandler, setStatusCommandHandler);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => log, log);

            _configuration = configuration;
            _rollbackCommand = rollbackCommand;
            _setStatusCommandHandler = setStatusCommandHandler;
            _headers = headers;
            _log = log.Create();
        }
        /// <summary>
        /// Rollbacks the specified message by rolling back the transaction
        /// </summary>
        /// <param name="context">The context.</param>
        public void RollbackForTransaction(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);
            //if transaction open, then just rollback the transaction
            if (connection.SqlConnection == null || connection.SqlTransaction == null) return;

            if (_configuration.Options().EnableStatusTable)
            {
                if (context.MessageId != null && context.MessageId.HasValue)
                {
                    _setStatusCommandHandler.Handle(new SetStatusTableStatusCommand((long) context.MessageId.Id.Value,
                        QueueStatuses.Waiting));
                }
            }
            try
            {
                connection.SqlTransaction.Rollback();
            }
            catch (Exception e)
            {
                _log.ErrorException("Failed to rollback a transaction; this might be due to a DB timeout", e);

                //don't attempt to use the transaction again at this point.
                connection.SqlTransaction = null;

                throw;
            }

            //ensure that transaction won't be used anymore
            connection.SqlTransaction.Dispose();
            connection.SqlTransaction = null;
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
