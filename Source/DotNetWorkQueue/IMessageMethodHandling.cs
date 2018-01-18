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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Handles processing of linq expression tree messages.
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IIsDisposed" />
    /// <seealso cref="System.IDisposable" />
    public interface IMessageMethodHandling: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Handles processing of linq expression tree messages.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        void HandleExecution(IReceivedMessage<MessageExpression> receivedMessage, IWorkerNotification workerNotification);
    }
}
