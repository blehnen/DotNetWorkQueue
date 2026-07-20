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
using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Interface for notification of consumer queue errors
    /// </summary>
    public interface IConsumerQueueErrorNotification
    {
        /// <summary>
        /// Error while processing a message
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeError(ErrorNotification error);
        /// <summary>
        /// Error while obtaining messages from transport
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeError(ErrorReceiveNotification error);
        /// <summary>
        /// A message has been moved to the error queue, if possible.
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeMovedToErrorQueue(ErrorNotification error);
        /// <summary>
        /// A poison message has been processed
        /// </summary>
        /// <param name="notification">Error information</param>
        void InvokePoisonMessageError(PoisonMessageNotification notification);

        /// <summary>
        /// Subscribe to notifications
        /// </summary>
        /// <param name="notifications">User notifications</param>
        void Sub(ConsumerQueueNotifications notifications);
    }
}
