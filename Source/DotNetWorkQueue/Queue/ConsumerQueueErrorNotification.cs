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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Notifications;

namespace DotNetWorkQueue.Queue
{
    internal class ConsumerQueueErrorNotification : IConsumerQueueErrorNotification
    {
        private readonly IConsumerMetricsNotification _metrics;
        private ConsumerQueueNotifications _notifications;

        public ConsumerQueueErrorNotification(IConsumerMetricsNotification metrics)
        {
            _metrics = metrics;
        }

        public void InvokeError(ErrorNotification error)
        {
            _metrics.IncrementErrored();
            _notifications?.Error?.Invoke(error);
        }

        public void InvokeError(ErrorReceiveNotification error)
        {
            _notifications?.ReceiveMessageError?.Invoke(error);
        }

        [SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "distinct domain events (error raised vs. moved to error queue); kept separate so they can diverge without touching callers")]
        public void InvokeMovedToErrorQueue(ErrorNotification error)
        {
            _metrics.IncrementErrored();
            _notifications?.Error?.Invoke(error);
        }

        public void InvokePoisonMessageError(PoisonMessageNotification notification)
        {
            _metrics.IncrementPoisonMessage();
            _notifications?.PoisonMessage?.Invoke(notification);
        }

        public void Sub(ConsumerQueueNotifications notifications)
        {
            _notifications = notifications;
        }
    }
}
