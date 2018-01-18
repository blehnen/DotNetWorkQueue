// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Error handling related to a unit of work
    /// </summary>
    public class ReceiveErrorMessage : IReceiveMessagesError
    {
        #region Member Level Variables
        private readonly ILog _log;
        private readonly IDataStorage _dataDataStorage;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveErrorMessage" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="dataDataStorage">The data data storage.</param>
        public ReceiveErrorMessage(
            ILogFactory log,
            IDataStorage dataDataStorage)
        {
            Guard.NotNull(() => dataDataStorage, dataDataStorage);
            Guard.NotNull(() => log, log);

            _log = log.Create();
            _dataDataStorage = dataDataStorage;
        }
        #endregion

        #region IReceiveMessagesError
        /// <summary>
        /// Invoked when a message has failed to process.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception)
        {
            //message failed to process
            if (context.MessageId == null || !context.MessageId.HasValue) return ReceiveMessagesErrorResult.NoActionPossible;

            _dataDataStorage.MoveToErrorQueue(exception, (Guid) context.MessageId.Id.Value, context);

            //we are done doing any processing - remove the messageID to block other actions
            context.MessageId = null;
            _log.ErrorException("Message with ID {0} has failed and has been moved to the error queue", exception,
                message.MessageId);
            return ReceiveMessagesErrorResult.Error;
        }
        #endregion
    }
}
