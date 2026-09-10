// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Message
{
    /// <summary>
    /// Rolls back a message by either rolling back a transaction or updating a status
    /// </summary>
    internal class RollbackMessage : ITransportRollbackMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICommandHandler<RollbackMessageCommand<long>> _rollbackCommand;
        private readonly ICommandHandlerAsync<RollbackMessageCommand<long>> _rollbackCommandAsync;
        private readonly ICommandHandler<SetStatusTableStatusCommand<long>> _setStatusCommandHandler;
        private readonly ICommandHandlerAsync<SetStatusTableStatusCommand<long>> _setStatusCommandHandlerAsync;
        private readonly IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> _headers;
        private readonly IIncreaseQueueDelay _increaseQueueDelay;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="rollbackCommand">The rollback command.</param>
        /// <param name="rollbackCommandAsync">The rollback command, for the asynchronous consumer.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="setStatusCommandHandlerAsync">The set status command handler, for the asynchronous consumer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="increaseQueueDelay">The increase queue delay.</param>
        public RollbackMessage(QueueConsumerConfiguration configuration,
            ICommandHandler<RollbackMessageCommand<long>> rollbackCommand,
            ICommandHandlerAsync<RollbackMessageCommand<long>> rollbackCommandAsync,
            ICommandHandler<SetStatusTableStatusCommand<long>> setStatusCommandHandler,
            ICommandHandlerAsync<SetStatusTableStatusCommand<long>> setStatusCommandHandlerAsync,
            IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> headers,
            IIncreaseQueueDelay increaseQueueDelay)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(rollbackCommand);
            Guard.NotNull(rollbackCommandAsync);
            Guard.NotNull(setStatusCommandHandler);
            Guard.NotNull(setStatusCommandHandlerAsync);
            Guard.NotNull(headers);
            Guard.NotNull(increaseQueueDelay);

            _configuration = configuration;
            _rollbackCommand = rollbackCommand;
            _rollbackCommandAsync = rollbackCommandAsync;
            _setStatusCommandHandler = setStatusCommandHandler;
            _setStatusCommandHandlerAsync = setStatusCommandHandlerAsync;
            _headers = headers;
            _increaseQueueDelay = increaseQueueDelay;
        }

        /// <summary>
        /// Rollbacks the specified message by setting the status
        /// </summary>
        /// <param name="context">The context.</param>
        public void Rollback(IMessageContext context)
        {
            if (HeldTransaction(context))
            {
                RollbackForTransaction(context);
                return;
            }

            if (TryBuildRollback(context, out var command))
            {
                _rollbackCommand.Handle(command);
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(IMessageContext context)
        {
            if (HeldTransaction(context))
            {
                await RollbackForTransactionAsync(context).ConfigureAwait(false);
                return;
            }

            if (TryBuildRollback(context, out var command))
            {
                await _rollbackCommandAsync.HandleAsync(command).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// True when the caller is holding a transaction, which is rolled back instead of the message.
        /// </summary>
        private bool HeldTransaction(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);
            return connection?.IsDisposed == false && connection.Connection != null && connection.Transaction != null;
        }

        /// <summary>
        /// Decides whether there is anything to roll back, and builds the command if so.
        /// </summary>
        /// <remarks>Shared by both members so the condition cannot drift between them.</remarks>
        private bool TryBuildRollback(IMessageContext context, out RollbackMessageCommand<long> command)
        {
            command = null;
            if (context.MessageId == null || !context.MessageId.HasValue) return false;

            //there is nothing to rollback unless at least one of these options is enabled
            if (!_configuration.Options().EnableDelayedProcessing &&
                !_configuration.Options().EnableHeartBeat &&
                !_configuration.Options().EnableStatus)
            {
                return false;
            }

            DateTime? lastHeartBeat = null;
            if (context.WorkerNotification?.HeartBeat?.Status?.LastHeartBeatTime != null)
            {
                lastHeartBeat = context.WorkerNotification.HeartBeat.Status.LastHeartBeatTime.Value;
            }

            var increaseDelay = context.Get(_increaseQueueDelay.QueueDelay).IncreaseDelay;
            command = new RollbackMessageCommand<long>(lastHeartBeat, (long)context.MessageId.Id.Value, increaseDelay);
            return true;
        }

        /// <summary>
        /// True when the status table also has to be reset before the transaction is rolled back.
        /// </summary>
        private bool ShouldResetStatus(IMessageContext context) =>
            _configuration.Options().EnableStatusTable && context.MessageId != null && context.MessageId.HasValue;

        /// <summary>
        /// Rollbacks the specified message by rolling back the transaction
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>
        /// Unlike SQL Server's equivalent this does not wrap the rollback in a try/catch, nor dispose
        /// and null the transaction afterwards. That asymmetry predates the asynchronous member and is
        /// mirrored rather than changed here; altering rollback behaviour on a held transaction is not
        /// something to slip into a conversion.
        /// </remarks>
        private void RollbackForTransaction(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);
            //if transaction open, then just rollback the transaction
            if (connection.Connection == null || connection.Transaction == null) return;

            if (ShouldResetStatus(context))
            {
                _setStatusCommandHandler.Handle(new SetStatusTableStatusCommand<long>((long)context.MessageId.Id.Value,
                    QueueStatuses.Waiting));
            }
            connection.Transaction.Rollback();
        }

        /// <summary>
        /// Rollbacks the specified message by rolling back the transaction, without blocking a thread.
        /// </summary>
        /// <param name="context">The context.</param>
        private async Task RollbackForTransactionAsync(IMessageContext context)
        {
            var connection = context.Get(_headers.Connection);
            //if transaction open, then just rollback the transaction
            if (connection.Connection == null || connection.Transaction == null) return;

            if (ShouldResetStatus(context))
            {
                await _setStatusCommandHandlerAsync.HandleAsync(
                    new SetStatusTableStatusCommand<long>((long)context.MessageId.Id.Value,
                        QueueStatuses.Waiting)).ConfigureAwait(false);
            }
            await connection.Transaction.RollbackAsync().ConfigureAwait(false);
        }

    }
}
