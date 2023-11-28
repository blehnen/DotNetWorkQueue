// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;
using System;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Handles moving poison messages to the error table
    /// </summary>
    public class ReceivePoisonMessage : IReceivePoisonMessage
    {
        private readonly IDataStorage _dataStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivePoisonMessage" /> class.
        /// </summary>
        /// <param name="dataStorage">The data storage.</param>
        public ReceivePoisonMessage(IDataStorage dataStorage)
        {
            Guard.NotNull(() => dataStorage, dataStorage);
            _dataStorage = dataStorage;
        }
        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occured during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public void Handle(IMessageContext context, PoisonMessageException exception)
        {
            Guard.NotNull(() => context, context);
            Guard.NotNull(() => exception, exception);

            if (context.MessageId == null || !context.MessageId.HasValue) return;

            _dataStorage.MoveToErrorQueue(exception, (Guid)context.MessageId.Id.Value, context);
            context.SetMessageAndHeaders(null, context.CorrelationId, context.Headers);
        }
    }
}
