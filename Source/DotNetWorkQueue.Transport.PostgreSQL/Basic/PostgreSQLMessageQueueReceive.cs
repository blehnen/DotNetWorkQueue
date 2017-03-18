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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Message;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Handles receive of messages, and passing them back to the caller
    /// </summary>
    internal class PostgreSqlMessageQueueReceive : IReceiveMessages
    {
        #region Member level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ICancelWork _cancelWork;

        private readonly ReceiveMessage _receiveMessages;
        private readonly HandleMessage _handleMessage;

        private readonly SqlHeaders _sqlHeaders;

        #endregion

        #region Delegates for the connection object
        /// <summary>
        /// Commits the message, using the information stored in the connection.
        /// </summary>
        Action<Connection, IMessageContext> _commitConnection;
        /// <summary>
        /// Roll back the message, using the information stored in the connection.
        /// </summary>
        Action<Connection, IMessageContext> _rollbackConnection;
        /// <summary>
        /// Calls dispose on the connection
        /// </summary>
        readonly Action<Connection> _disposeConnection;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueReceive" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="handleMessage">The handle message.</param>
        /// <param name="receiveMessages">The receive messages.</param>
        /// <param name="sqlHeaders">The SQL headers.</param>
        public PostgreSqlMessageQueueReceive(QueueConsumerConfiguration configuration,
            IConnectionFactory connectionFactory,
            IQueueCancelWork cancelWork,
            HandleMessage handleMessage,
            ReceiveMessage receiveMessages,
            SqlHeaders sqlHeaders)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => connectionFactory, connectionFactory);
            Guard.NotNull(() => cancelWork, cancelWork);
            Guard.NotNull(() => handleMessage, handleMessage);
            Guard.NotNull(() => receiveMessages, receiveMessages);
            Guard.NotNull(() => sqlHeaders, sqlHeaders);

            _configuration = configuration;
            _connectionFactory = connectionFactory;
            _cancelWork = cancelWork;
            _handleMessage = handleMessage;
            _receiveMessages = receiveMessages;
            _sqlHeaders = sqlHeaders;
            _disposeConnection = c => c.Dispose(); 

        }
        #endregion

        #region IReceiveMessages

        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        /// <exception cref="ReceiveMessageException">An error occurred while attempting to read messages from the queue</exception>
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommited)
            {
                _commitConnection = (c, b) => _handleMessage.CommitMessage.CommitForTransaction(context);
                _rollbackConnection = (c, b) => _handleMessage.RollbackMessage.RollbackForTransaction(context);
            }

            try
            {
                if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
                {
                    return null;
                }

                if (_configuration.Options().QueueType == QueueTypes.RpcReceive)
                {
                    var rpc = context.Get(_configuration.HeaderNames.StandardHeaders.RpcContext);
                    if (rpc.MessageId == null || !rpc.MessageId.HasValue)
                    {
                        return null;
                    }
                }

                var connection = GetConnectionAndSetOnContext(context);
                try
                {
                    return _receiveMessages.GetMessage(context, connection, connection1 => _disposeConnection(connection), _configuration.Routes);
                }
                finally
                {
                    if (!_configuration.Options().EnableHoldTransactionUntilMessageCommited)
                    {
                        _disposeConnection(connection);
                    }
                }
            }
            catch (PoisonMessageException exception)
            {
                if (exception.MessageId != null && exception.MessageId.HasValue)
                {
                    context.MessageId = exception.MessageId;
                }
                throw;
            }
            catch (Exception exception)
            {
                throw new ReceiveMessageException("An error occurred while attempting to read messages from the queue",
                    exception);
            }
        }

        /// <summary>
        /// Returns a message to process.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <returns>
        /// A message to process or null if there are no messages to process
        /// </returns>
        /// <exception cref="ReceiveMessageException">An error occurred while attempting to read messages from the queue</exception>
        public async Task<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context)
        {
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommited)
            {
                _commitConnection = (c, b) => _handleMessage.CommitMessage.CommitForTransaction(context);
                _rollbackConnection = (c, b) => _handleMessage.RollbackMessage.RollbackForTransaction(context);
            }

            try
            {
                if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
                {
                    return null;
                }

                if (_configuration.Options().QueueType == QueueTypes.RpcReceive)
                {
                    var rpc = context.Get(_configuration.HeaderNames.StandardHeaders.RpcContext);
                    if (rpc.MessageId == null || !rpc.MessageId.HasValue)
                    {
                        return null;
                    }
                }

                var connection = GetConnectionAndSetOnContext(context);
                try
                {
                    return await _receiveMessages.GetMessageAsync(context, connection, connection1 => _disposeConnection(connection), _configuration.Routes).ConfigureAwait(false);
                }
                finally
                {
                    if (!_configuration.Options().EnableHoldTransactionUntilMessageCommited)
                    {
                        _disposeConnection(connection);
                    }
                }
            }
            catch (PoisonMessageException exception)
            {
                if (exception.MessageId != null && exception.MessageId.HasValue)
                {
                    context.MessageId = exception.MessageId;
                }
                throw;
            }
            catch (Exception exception)
            {
                throw new ReceiveMessageException("An error occurred while attempting to read messages from the queue",
                    exception);
            }
        }

        #endregion

        #region Private Methods   
        /// <summary>
        /// Creates the connection object for the parent caller and stores it on the worker context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private Connection GetConnectionAndSetOnContext(IMessageContext context)
        {
            var connection = _connectionFactory.Create();
            context.Set(_sqlHeaders.Connection, connection);

            //wire up the context commit/rollback/dispose delegates
            if (!_configuration.Options().EnableHoldTransactionUntilMessageCommited)
            {
                context.Commit += ContextOnCommit;
                context.Rollback += ContextOnRollback;
            }
            else
            {
                context.Commit += ContextOnCommitTransaction;
                context.Rollback += ContextOnRollbackTransaction;
            }
            context.Cleanup += context_Cleanup;
            return connection;
        }

        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void context_Cleanup(object sender, EventArgs e)
        {
            var context = (IMessageContext) sender;
            var connection = context.Get(_sqlHeaders.Connection);
            ContextCleanup(context, connection);
        }

        /// <summary>
        /// Roll back a message
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnRollbackTransaction(object sender, EventArgs eventArgs)
        {
            Guard.NotNull(() => _rollbackConnection, _rollbackConnection);
            Guard.NotNull(() => sender, sender);

            var context = (IMessageContext)sender;
            var connection = context.Get(_sqlHeaders.Connection);
            _rollbackConnection(connection, context);
        }

        /// <summary>
        /// Commit a message
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnCommitTransaction(object sender, EventArgs eventArgs)
        {
            Guard.NotNull(() => _commitConnection, _commitConnection);
            Guard.NotNull(() => sender, sender);

            var context = (IMessageContext)sender;
            var connection = context.Get(_sqlHeaders.Connection);
            _commitConnection(connection, context);
        }

        /// <summary>
        /// On Rollback
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnRollback(object sender, EventArgs eventArgs)
        {
            _handleMessage.RollbackMessage.Rollback((IMessageContext)sender);
        }

        /// <summary>
        /// On Commit
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnCommit(object sender, EventArgs eventArgs)
        {
            _handleMessage.CommitMessage.Commit((IMessageContext)sender);
        }

        /// <summary>
        /// Clean up the message context when processing is done
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="connection">The connection.</param>
        private void ContextCleanup(IMessageContext context, Connection connection)
        {
            if (!_configuration.Options().EnableHoldTransactionUntilMessageCommited)
            {
                context.Commit -= ContextOnCommit;
                context.Rollback -= ContextOnRollback;
            }
            else
            {
                context.Commit -= ContextOnCommitTransaction;
                context.Rollback -= ContextOnRollbackTransaction;
            }
            context.Cleanup -= context_Cleanup;
            _disposeConnection(connection);
        }
        #endregion
    }
}
