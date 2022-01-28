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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessage" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="receiveMessage">The receive message.</param>
        /// <param name="cancelToken">The cancel token.</param>
        public ReceiveMessage(QueueConsumerConfiguration configuration,
            IQueryHandler<ReceiveMessageQuery<IDbConnection, IDbTransaction>, IReceivedMessageInternal> receiveMessage,
            IQueueCancelWork cancelToken)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => receiveMessage, receiveMessage);
            Guard.NotNull(() => cancelToken, cancelToken);

            _configuration = configuration;
            _receiveMessage = receiveMessage;
            _cancelToken = cancelToken;
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

            //ask for the next message
            var receivedTransportMessage =
                _receiveMessage.Handle(new ReceiveMessageQuery<IDbConnection, IDbTransaction>(null, null, _configuration.Routes, _configuration.GetUserParameters(), _configuration.GetUserClause()));

            //if no message (null) run the no message action and return
            if (receivedTransportMessage == null)
            {
                return null;
            }

            //set the message ID on the context for later usage
            context.SetMessageAndHeaders(receivedTransportMessage.MessageId, receivedTransportMessage.Headers);

            return receivedTransportMessage;
        }
    }
}
