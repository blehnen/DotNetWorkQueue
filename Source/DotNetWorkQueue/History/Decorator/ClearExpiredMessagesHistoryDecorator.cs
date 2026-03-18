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
using System.Threading;

namespace DotNetWorkQueue.History.Decorator
{
    /// <summary>
    /// Note: The clear expired messages handler operates on batches of message IDs,
    /// but the transport-level implementation deletes them without exposing which IDs were cleared.
    /// History tracking for expired messages would require transport-level changes to return the cleared IDs.
    /// For now, this decorator is a pass-through — expired message history is best tracked
    /// at the consumer level when a message is detected as expired during dequeue.
    /// </summary>
    internal class ClearExpiredMessagesHistoryDecorator : IClearExpiredMessages
    {
        private readonly IClearExpiredMessages _handler;

        public ClearExpiredMessagesHistoryDecorator(IClearExpiredMessages handler)
        {
            _handler = handler;
        }

        public long ClearMessages(CancellationToken cancelToken)
        {
            return _handler.ClearMessages(cancelToken);
        }
    }
}
