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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Handles receive of messages, and passing them back to the caller
    /// </summary>
    internal class SqLiteMessageQueueReceive : IReceiveMessages
    {
        #region Member level Variables
        private readonly QueueConsumerConfiguration _configuration;
        private readonly ICancelWork _cancelWork;

        private readonly ReceiveMessage _receiveMessages;
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;
        private readonly DatabaseExists _databaseExists;
        private readonly ITransportHandleMessage _handleMessage;
        private readonly ILogger _log;
        private readonly SqLiteHeaders _sqLiteHeaders;
        private static bool _loggedMissingDb;
        private static readonly object LoggedMissingDbLock = new object();

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueReceive" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="handleMessage">The handle message.</param>
        /// <param name="receiveMessages">The receive messages.</param>
        /// <param name="log">The log.</param>
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        /// <param name="databaseExists">The database exists.</param>
        /// <param name="sqLiteHeaders">Typed key resolver for reading hold-transaction connection state off the context (Phase 5).</param>
        public SqLiteMessageQueueReceive(QueueConsumerConfiguration configuration,
            IQueueCancelWork cancelWork,
            ITransportHandleMessage handleMessage,
            ReceiveMessage receiveMessages,
            ILogger log,
            IGetFileNameFromConnectionString getFileNameFromConnection,
            DatabaseExists databaseExists,
            SqLiteHeaders sqLiteHeaders)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => cancelWork, cancelWork);
            Guard.NotNull(() => handleMessage, handleMessage);
            Guard.NotNull(() => receiveMessages, receiveMessages);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            Guard.NotNull(() => databaseExists, databaseExists);
            Guard.NotNull(() => sqLiteHeaders, sqLiteHeaders);

            _log = log;
            _configuration = configuration;
            _cancelWork = cancelWork;
            _handleMessage = handleMessage;
            _receiveMessages = receiveMessages;
            _getFileNameFromConnection = getFileNameFromConnection;
            _databaseExists = databaseExists;
            _sqLiteHeaders = sqLiteHeaders;
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
            try
            {
                return ReceiveSharedLogic(context) ? _receiveMessages.GetMessage(context) : null;
            }
            catch (PoisonMessageException exception)
            {
                if (exception.MessageId != null && exception.MessageId.HasValue)
                {
                    context.SetMessageAndHeaders(exception.MessageId, context.CorrelationId, context.Headers);
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
        public bool IsBlockingOperation => false; //nope

        /// <summary>
        /// Performs pre-checks on context
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private bool ReceiveSharedLogic(IMessageContext context)
        {
            if (!LoggedMissingDb && !_databaseExists.Exists(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString))
            {
                _log.LogWarning($"Database file {_getFileNameFromConnection.GetFileName(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString).FileName} does not exist");
                LoggedMissingDb = true;
            }

            if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
            {
                return false;
            }

            SetActionsOnContext(context);
            return true;
        }
        #endregion

        public static bool LoggedMissingDb
        {
            get
            {
                lock (LoggedMissingDbLock)
                {
                    return _loggedMissingDb;
                }
            }
            set
            {
                lock (LoggedMissingDbLock)
                {
                    _loggedMissingDb = value;
                }
            }
        }

        #region Private Methods   

        /// <summary>
        /// Creates the connection object for the parent caller and stores it on the worker context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private void SetActionsOnContext(IMessageContext context)
        {
            //wire up the context commit/rollback/dispose delegates
            context.Commit += ContextOnCommit;
            context.Rollback += ContextOnRollback;
            context.Cleanup += Context_Cleanup;
        }

        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Context_Cleanup(object sender, EventArgs e)
        {
            var context = (IMessageContext)sender;
            ContextCleanup(context);
        }

        /// <summary>
        /// On Rollback
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnRollback(object sender, EventArgs eventArgs)
        {
            var context = (IMessageContext)sender;
            _handleMessage.RollbackMessage.Rollback(context);

            // Phase 5: when EnableHoldTransactionUntilMessageCommitted = true, the dequeue
            // transaction was created in ReceiveMessage.GetMessage and stored on the
            // context via SqLiteHeaders.ConnectionState. Roll it back here; Context_Cleanup
            // disposes the resources. MarkCompleted's atomic CAS is the race-free gate;
            // a non-zero check-then-act would leave a window where commit and rollback
            // could both fire.
            var state = context.Get(_sqLiteHeaders.ConnectionState);
            if (state != null && state.MarkCompleted())
            {
                state.Transaction.Rollback();
            }
        }

        /// <summary>
        /// On Commit
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="eventArgs">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void ContextOnCommit(object sender, EventArgs eventArgs)
        {
            var context = (IMessageContext)sender;
            _handleMessage.CommitMessage.Commit(context);

            // Phase 5: commit the held dequeue transaction (hold-transaction path) after the
            // library-side commit-message bookkeeping completes successfully. The user
            // handler's business writes (via the inbox SqLiteRelationalWorkerNotification.Transaction
            // capability) commit atomically with the dequeue here. MarkCompleted's atomic
            // CAS is the race-free gate (see ContextOnRollback for the symmetric note).
            var state = context.Get(_sqLiteHeaders.ConnectionState);
            if (state != null && state.MarkCompleted())
            {
                state.Transaction.Commit();
            }
        }

        /// <summary>
        /// Clean up the message context when processing is done
        /// </summary>
        /// <param name="context">The context.</param>
        private void ContextCleanup(IMessageContext context)
        {
            // Phase 5: release the held dequeue resources (hold-transaction path). Commit/rollback
            // already happened in ContextOnCommit / ContextOnRollback if applicable; this
            // just disposes. Safe to call even if the state was never set (option=false).
            // Unsubscribe in a finally so a Dispose() throw can't leak handler subscriptions
            // and re-fire on future receives.
            try
            {
                var state = context.Get(_sqLiteHeaders.ConnectionState);
                if (state != null)
                {
                    state.Transaction.Dispose();
                    state.Connection.Dispose();
                }
            }
            finally
            {
                context.Commit -= ContextOnCommit;
                context.Rollback -= ContextOnRollback;
                context.Cleanup -= Context_Cleanup;
            }
        }

        #endregion
    }
}
