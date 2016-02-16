// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.Logging.Decorator
{
    internal class ReceivePoisonMessageDecorator: IReceivePoisonMessage
    {
        private readonly ILog _log;
        private readonly IReceivePoisonMessage _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivePoisonMessageDecorator" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="handler">The handler.</param>
        public ReceivePoisonMessageDecorator(ILogFactory log,
            IReceivePoisonMessage handler)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => handler, handler);

            _log = log.Create();
            _handler = handler;
        }

        /// <summary>
        /// Invoked when we have dequeued a message, but a failure occured during re-assembly.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        public void Handle(IMessageContext context, Exception exception)
        {
            if (context.MessageId != null && context.MessageId.HasValue)
            {
                var messageId = context.MessageId.Id.Value.ToString();
                _handler.Handle(context, exception);
                _log.ErrorException(
                    "Message with ID {0} has failed after de-queue, but before finishing loading. This message is considered a poison message, and has been moved to the error queue",
                    exception,
                    messageId);
            }
            else
            {
                _handler.Handle(context, exception);
            }
        }
    }
}
