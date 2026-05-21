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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using System;
using System.Data;
using System.Linq;

namespace DotNetWorkQueue.Transport.SQLite.Basic.Message
{
    /// <summary>
    /// Handles receiving a message
    /// </summary>
    internal class ReceiveMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal> _receiveMessage;
        private readonly ICancelWork _cancelToken;
        private readonly IDbFactory _dbFactory;
        private readonly IConnectionInformation _connectionInformation;
        // Hold a direct reference to the options factory instead of caching its result in a
        // Lazy<T>: the factory itself caches the persisted options after the first DB read
        // (SqLiteMessageQueueTransportOptionsFactory._options field), but its first call may
        // resolve before the queue's options have been persisted, leaving a Lazy stuck on a
        // default options instance for the lifetime of this ReceiveMessage. Calling the
        // factory each receive is cheap (cached after first hit) and guarantees the receive
        // path observes the same option value as the IWorkerNotification registration lambda.
        private readonly ISqLiteMessageQueueTransportOptionsFactory _optionsFactory;
        private readonly SqLiteHeaders _sqLiteHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="cancelToken">The cancel token.</param>
        /// <param name="dbFactory">The db factory used to create connections + transactions in hold-transaction mode.</param>
        /// <param name="connectionInformation">Connection info for hold-transaction mode connection creation.</param>
        /// <param name="optionsFactory">Options factory; consulted to detect hold-transaction mode.</param>
        /// <param name="sqLiteHeaders">Typed key for storing per-message connection state on the context in hold-transaction mode.</param>
        public ReceiveMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal> receiveMessage,
            IQueueCancelWork cancelToken,
            IDbFactory dbFactory,
            IConnectionInformation connectionInformation,
            ISqLiteMessageQueueTransportOptionsFactory optionsFactory,
            SqLiteHeaders sqLiteHeaders)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => cancelToken, cancelToken);
            Guard.NotNull(() => dbFactory, dbFactory);
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => sqLiteHeaders, sqLiteHeaders);

            _configuration = configuration;
            _receiveMessage = receiveMessage;
            _cancelToken = cancelToken;
            _dbFactory = dbFactory;
            _connectionInformation = connectionInformation;
            _optionsFactory = optionsFactory;
            _sqLiteHeaders = sqLiteHeaders;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public IReceivedMessageInternal GetMessage(IMessageContext context)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                return null;
            }

            // Phase 5: when EnableHoldTransactionUntilMessageCommitted = true, create the
            // connection + transaction HERE so they outlive the query handler call. The
            // receive-path commit/rollback/cleanup delegates (in SqLiteMessageQueueReceive)
            // will read the state from context and complete the lifecycle after the user
            // handler returns.
            IDbConnection heldConnection = null;
            IDbTransaction heldTransaction = null;
            var options = _optionsFactory.Create();
            if (options.EnableHoldTransactionUntilMessageCommitted)
            {
                heldConnection = _dbFactory.CreateConnection(_connectionInformation.ConnectionString, false);
                try
                {
                    heldConnection.Open();
                    heldTransaction = _dbFactory.CreateTransaction(heldConnection).BeginTransaction();
                }
                catch
                {
                    heldConnection.Dispose();
                    throw;
                }
            }

            IReceivedMessageInternal receivedTransportMessage;
            try
            {
                receivedTransportMessage = _receiveMessage.Handle(
                    new ReceiveMessageQuery<IDbConnection, IDbTransaction>(
                        heldConnection, heldTransaction,
                        _configuration.Routes,
                        _configuration.GetUserParameters(),
                        _configuration.GetUserClause()));
            }
            catch
            {
                // hold-transaction path: ensure we don't leak the connection/transaction on Handle failure
                heldTransaction?.Dispose();
                heldConnection?.Dispose();
                throw;
            }

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                // hold-transaction path with no message: nothing to commit; release resources now
                heldTransaction?.Dispose();
                heldConnection?.Dispose();
                return null;
            }

            try
            {
                //set the message ID on the context for later usage
                context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.CorrelationId, receivedTransportMessage.Headers);

                // hold-transaction path with a message: store the state on context so the receive-class
                // commit/rollback/cleanup delegates can finish the lifecycle after the user handler.
                // Also inject the state into the resolved IWorkerNotification if it is the
                // relational variant (mirrors SqlServer/PostgreSQL Phase 3/4 pattern). The same
                // context.WorkerNotification reference flows from MessageContext.ctor through to
                // ProcessMessage.Handle, so the property set here is visible to the user handler.
                if (heldConnection != null && heldTransaction != null)
                {
                    var state = new SqLiteConnectionState(heldConnection, heldTransaction);
                    context.Set(_sqLiteHeaders.ConnectionState, state);
                    if (context.WorkerNotification is SqLiteRelationalWorkerNotification relationalNotification)
                    {
                        relationalNotification.ConnectionState = state;
                    }
                }
            }
            catch
            {
                // hold-transaction path: SetMessageAndHeaders / context.Set / notification injection threw.
                // The cleanup delegates won't fire (context state never installed), so release the
                // held connection + transaction here to avoid a resource leak.
                heldTransaction?.Dispose();
                heldConnection?.Dispose();
                throw;
            }

            return receivedTransportMessage;
        }
    }
}
