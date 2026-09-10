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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Message;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    internal class PostgreSqlMessageQueueReceive : IReceiveMessages
    {
        #region Member level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly IConnectionHolderFactory<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> _connectionFactory;
        private readonly ICancelWork _cancelWork;

        private readonly ReceiveMessage _receiveMessages;
        private readonly ITransportHandleMessage _handleMessage;

        private readonly IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> _sqlHeaders;

        #endregion

        #region Delegates for the connection object
        /// <summary>
        /// Commits the message, using the information stored in the connection.
        /// </summary>
        Action<IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>, IMessageContext> _commitConnection;
        /// <summary>
        /// Roll back the message, using the information stored in the connection.
        /// </summary>
        Action<IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>, IMessageContext> _rollbackConnection;
        /// <summary>
        /// Calls dispose on the connection
        /// </summary>
        readonly Action<IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>> _disposeConnection;
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
            IConnectionHolderFactory<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> connectionFactory,
            IQueueCancelWork cancelWork,
            ITransportHandleMessage handleMessage,
            ReceiveMessage receiveMessages,
            IConnectionHeader<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> sqlHeaders)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(connectionFactory);
            Guard.NotNull(cancelWork);
            Guard.NotNull(handleMessage);
            Guard.NotNull(receiveMessages);
            Guard.NotNull(sqlHeaders);

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

        /// <inheritdoc />
        public IReceivedMessageInternal ReceiveMessage(IMessageContext context)
        {
            if (_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
            {
                _commitConnection = (c, b) => _handleMessage.CommitMessage.Commit(context);
                _rollbackConnection = (c, b) => _handleMessage.RollbackMessage.Rollback(context);
            }

            try
            {
                if (_cancelWork.AnyCancellationRequested())
                {
                    return null;
                }

                var connection = GetConnectionAndSetOnContext(context);
                try
                {
                    return _receiveMessages.GetMessage(context, connection, connection1 => _disposeConnection(connection), _configuration.Routes, _configuration.GetUserParameters(), _configuration.GetUserClause());
                }
                finally
                {
                    if (!_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
                    {
                        _disposeConnection(connection);
                    }
                }
            }
            catch (PoisonMessageException exception)
            {
                if (exception.MessageId != null && exception.MessageId.HasValue)
                {
                    context.SetMessageAndHeaders(exception.MessageId, exception.CorrelationId, exception.Headers);
                }
                throw;
            }
            catch (Exception exception)
            {
                throw new ReceiveMessageException("An error occurred while attempting to read messages from the queue",
                    exception);
            }
        }

        /// <inheritdoc />
        public ValueTask<IReceivedMessageInternal> ReceiveMessageAsync(IMessageContext context, CancellationToken cancellation)
        {
            //TEMPORARY - replaced with a genuinely asynchronous implementation later in this
            //PR. Deliberately NOT Task.Run: that would move the block to a different pool
            //thread while looking like a fix.
            return new ValueTask<IReceivedMessageInternal>(ReceiveMessage(context));
        }

        /// <inheritdoc />
        public bool IsBlockingOperation => false; //nope

        #endregion

        #region Private Methods   
        //Cached so the wiring below does not build a delegate for each of these method groups on
        //every message: a subscribe and an unsubscribe each built their own, six per message. One
        //instance of this class serves one worker, and even shared the worst case is a duplicate
        //delegate that unsubscribes just as well, since removal compares target and method rather
        //than reference.
        private EventHandler _cachedCommit;
        private AsyncEventHandler _cachedCommitAsync;
        private AsyncEventHandler _cachedRollbackAsync;
        private EventHandler _cachedCommitTransaction;
        private EventHandler _cachedRollback;
        private EventHandler _cachedRollbackTransaction;
        private EventHandler _cachedCleanup;

        /// <summary>
        /// Creates the connection object for the parent caller and stores it on the worker context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> GetConnectionAndSetOnContext(IMessageContext context)
        {
            var connection = _connectionFactory.Create();
            context.Set(_sqlHeaders.Connection, connection);

            //wire up the context commit/rollback/dispose delegates
            if (!_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
            {
                context.Commit += _cachedCommit ??= ContextOnCommit;
                //Both are subscribed: the synchronous consumer raises Commit, the asynchronous one
                //raises CommitAsync. A context only ever raises one of them.
                context.CommitAsync += _cachedCommitAsync ??= ContextOnCommitAsync;
                context.Rollback += _cachedRollback ??= ContextOnRollback;
                context.RollbackAsync += _cachedRollbackAsync ??= ContextOnRollbackAsync;
            }
            else
            {
                context.Commit += _cachedCommitTransaction ??= ContextOnCommitTransaction;
                //The same awaited handler serves both branches: the held-transaction commit delegate
                //also ends at CommitMessage.Commit, and RemoveAsync handles the held transaction
                //itself. Missing this is what left the asynchronous consumer unable to commit with
                //EnableHoldTransactionUntilMessageCommitted on.
                context.CommitAsync += _cachedCommitAsync ??= ContextOnCommitAsync;
                context.Rollback += _cachedRollbackTransaction ??= ContextOnRollbackTransaction;
                context.RollbackAsync += _cachedRollbackAsync ??= ContextOnRollbackAsync;
            }
            context.Cleanup += _cachedCleanup ??= Context_Cleanup;

            // Phase 4 inbox: if the resolved IWorkerNotification is the relational variant (selected
            // by PostgreSQLMessageQueueInit's factory delegate when EnableHoldTransactionUntilMessageCommitted
            // is true), inject the per-message ConnectionHolder so the user handler can read
            // the active dequeue transaction via the IRelationalWorkerNotification capability cast.
            // When the option is false, context.WorkerNotification is a plain WorkerNotification and the
            // pattern-match fails — no-op, no harm.
            if (context.WorkerNotification is PostgreSqlRelationalWorkerNotification relationalNotification)
            {
                relationalNotification.ConnectionHolder = connection;
            }

            return connection;
        }

        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Context_Cleanup(object sender, EventArgs e)
        {
            var context = (IMessageContext)sender;
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
            Guard.NotNull(_rollbackConnection);
            Guard.NotNull(sender);

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
            Guard.NotNull(_commitConnection);
            Guard.NotNull(sender);

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
        /// On Commit, awaited
        /// </summary>
        private async Task ContextOnCommitAsync(object sender, EventArgs eventArgs)
        {
            await _handleMessage.CommitMessage.CommitAsync((IMessageContext)sender).ConfigureAwait(false);
        }

        /// <summary>
        /// On Rollback, awaited
        /// </summary>
        /// <remarks>
        /// One handler serves every subscription site, including the held-transaction branch
        /// where the synchronous path goes through a separate delegate: RollbackAsync detects a
        /// held transaction itself and rolls that back instead of the message.
        /// </remarks>
        private async Task ContextOnRollbackAsync(object sender, EventArgs eventArgs)
        {
            await _handleMessage.RollbackMessage.RollbackAsync((IMessageContext)sender).ConfigureAwait(false);
        }

        /// <summary>
        /// Clean up the message context when processing is done
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="connectionHolder">The connection.</param>
        private void ContextCleanup(IMessageContext context, IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> connectionHolder)
        {
            if (!_configuration.Options().EnableHoldTransactionUntilMessageCommitted)
            {
                context.Commit -= _cachedCommit;
                context.Rollback -= _cachedRollback;
            }
            else
            {
                context.Commit -= _cachedCommitTransaction;
                context.Rollback -= _cachedRollbackTransaction;
            }
            context.CommitAsync -= _cachedCommitAsync;
            context.RollbackAsync -= _cachedRollbackAsync;
            context.Cleanup -= _cachedCleanup;
            _disposeConnection(connectionHolder);
        }
        #endregion
    }
}
