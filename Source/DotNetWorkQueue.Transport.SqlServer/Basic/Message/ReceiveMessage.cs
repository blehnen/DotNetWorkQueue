﻿// ---------------------------------------------------------------------
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
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Message
{
    /// <summary>
    /// Handles receiving a message
    /// </summary>
    internal class ReceiveMessage
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IQueryHandler<ReceiveMessageQuery<SqlConnection, SqlTransaction>, IReceivedMessageInternal> _receiveMessage;
        private readonly ICommandHandler<SetStatusTableStatusCommand<long>> _setStatusCommandHandler;
        private readonly ICancelWork _cancelToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="setStatusCommandHandler">The set status command handler.</param>
        /// <param name="cancelToken">The cancel token.</param>
        public ReceiveMessage(QueueConsumerConfiguration configuration, IQueryHandler<ReceiveMessageQuery<SqlConnection, SqlTransaction>, IReceivedMessageInternal> receiveMessage,
            ICommandHandler<SetStatusTableStatusCommand<long>> setStatusCommandHandler,
            IQueueCancelWork cancelToken)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => setStatusCommandHandler, setStatusCommandHandler);
            Guard.NotNull(() => cancelToken, cancelToken);

            _configuration = configuration;
            _receiveMessage = receiveMessage;
            _setStatusCommandHandler = setStatusCommandHandler;
            _cancelToken = cancelToken;
        }

        /// <summary>
        /// Returns the next message, if any.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="connectionHolder">The connection.</param>
        /// <param name="noMessageFoundActon">The no message found action.</param>
        /// <returns>
        /// A message if one is found; null otherwise
        /// </returns>
        public IReceivedMessageInternal GetMessage(IMessageContext context, IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> connectionHolder,
            Action<IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand>> noMessageFoundActon)
        {
            //if stopping, exit now
            if (_cancelToken.Tokens.Any(t => t.IsCancellationRequested))
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //ask for the next message
            var receivedTransportMessage =
                _receiveMessage.Handle(new ReceiveMessageQuery<SqlConnection, SqlTransaction>(connectionHolder.Connection,
                    connectionHolder.Transaction,  _configuration.Routes, _configuration.GetUserParameters(), _configuration.GetUserClause()));

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                noMessageFoundActon(connectionHolder);
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            //if we are holding open transactions, we need to update the status table in a separate call
            //When not using held transactions, this is part of the de-queue statement and so not needed here

            //TODO - we could consider using a task to update the status table
            //the status table drives nothing internally, however it may drive external processes
            //because of that, we are not returning the message until the status table is updated.
            //we could make this a configurable option in the future?
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommitted &&
                _configuration.Options().EnableStatusTable)
            {
                _setStatusCommandHandler.Handle(
                    new SetStatusTableStatusCommand<long>(
                        (long) receivedTransportMessage.MessageId.Id.Value, QueueStatuses.Processing));
            }
            return receivedTransportMessage;
        }
    }
}
