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
    /// Notification for consumer queue message processing
    /// </summary>
    public interface IConsumerQueueNotification
    {
        /// <summary>
        /// The message has been rolled back for re-processing, if possible
        /// </summary>
        /// <param name="rollbackNotification">The rollback information</param>
        void InvokeRollback(RollBackNotification rollbackNotification);

        /// <summary>
        /// The message has completed processing.
        /// </summary>
        /// <param name="messageCompleteNotification">The message that has been completed</param>
        void InvokeMessageComplete(MessageCompleteNotification messageCompleteNotification);

        /// <summary>
        /// Subscribe for user notifications
        /// </summary>
        /// <param name="notifications">User notifications</param>
        void Sub(ConsumerQueueNotifications notifications);
    }
}
