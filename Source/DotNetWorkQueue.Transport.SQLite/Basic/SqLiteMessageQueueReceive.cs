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
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic.Message;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

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
        private readonly HandleMessage _handleMessage;
        private readonly ILog _log;
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
        public SqLiteMessageQueueReceive(QueueConsumerConfiguration configuration,
            IQueueCancelWork cancelWork,
            HandleMessage handleMessage,
            ReceiveMessage receiveMessages,
            ILogFactory log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => cancelWork, cancelWork);
            Guard.NotNull(() => handleMessage, handleMessage);
            Guard.NotNull(() => receiveMessages, receiveMessages);
            Guard.NotNull(() => log, log);

            _log = log.Create();
            _configuration = configuration;
            _cancelWork = cancelWork;
            _handleMessage = handleMessage;
            _receiveMessages = receiveMessages;
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
            try
            {
                if (ReceiveSharedLogic(context))
                {
                    return await _receiveMessages.GetMessageAsync(context).ConfigureAwait(false);
                }
                return null;
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
        /// Performs prechecks on context
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        private bool ReceiveSharedLogic(IMessageContext context)
        {
            if (!LoggedMissingDb && !DatabaseExists.Exists(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString))
            {
                _log.WarnFormat("Database file {0} does not exist", GetFileNameFromConnectionString.GetFileName(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString).FileName);
                LoggedMissingDb = true;
            }

            if (_cancelWork.Tokens.Any(m => m.IsCancellationRequested))
            {
                return false;
            }

            if (_configuration.Options().QueueType == QueueTypes.RpcReceive)
            {
                var rpc = context.Get(_configuration.HeaderNames.StandardHeaders.RpcContext);
                if (rpc.MessageId == null || !rpc.MessageId.HasValue)
                {
                    return false;
                }
            }

            SetActionsOnContext(context);
            return true;
        }
        #endregion

        public static bool LoggedMissingDb
        {
            get
            {
                lock(LoggedMissingDbLock)
                {
                    return _loggedMissingDb;
                }
            }
            set
            {
                lock(LoggedMissingDbLock)
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
            context.Cleanup += context_Cleanup;
        }

        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void context_Cleanup(object sender, EventArgs e)
        {
            var context = (IMessageContext) sender;
            ContextCleanup(context);
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
        private void ContextCleanup(IMessageContext context)
        {
            context.Commit -= ContextOnCommit;
            context.Rollback -= ContextOnRollback;
            context.Cleanup -= context_Cleanup;
        }

        #endregion
    }
}
