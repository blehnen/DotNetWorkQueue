using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic.Message;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
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
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        /// <param name="databaseExists">The database exists.</param>
        public SqLiteMessageQueueReceive(QueueConsumerConfiguration configuration,
            IQueueCancelWork cancelWork,
            HandleMessage handleMessage,
            ReceiveMessage receiveMessages,
            ILogFactory log,
            IGetFileNameFromConnectionString getFileNameFromConnection,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => cancelWork, cancelWork);
            Guard.NotNull(() => handleMessage, handleMessage);
            Guard.NotNull(() => receiveMessages, receiveMessages);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            Guard.NotNull(() => databaseExists, databaseExists);

            _log = log.Create();
            _configuration = configuration;
            _cancelWork = cancelWork;
            _handleMessage = handleMessage;
            _receiveMessages = receiveMessages;
            _getFileNameFromConnection = getFileNameFromConnection;
            _databaseExists = databaseExists;
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
                    context.SetMessageAndHeaders(exception.MessageId, context.Headers);
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
                    context.SetMessageAndHeaders(exception.MessageId, context.Headers);
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
                _log.WarnFormat("Database file {0} does not exist", _getFileNameFromConnection.GetFileName(_configuration.TransportConfiguration.ConnectionInfo.ConnectionString).FileName);
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
            context.Cleanup += Context_Cleanup;
        }

        /// <summary>
        /// Handles the Cleanup event of the context control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Context_Cleanup(object sender, EventArgs e)
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
            context.Cleanup -= Context_Cleanup;
        }

        #endregion
    }
}
