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

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// No-op implementation of <see cref="IWriteMessageHistory"/>. Used when history is disabled.
    /// </summary>
    public class WriteMessageHistoryNoOp : IWriteMessageHistory
    {
        /// <inheritdoc />
        public void RecordEnqueue(string queueId, string correlationId, string route, string messageType,
            byte[] body, byte[] headers) { }

        /// <inheritdoc />
        public void RecordProcessingStart(string queueId) { }

        /// <inheritdoc />
        public void RecordComplete(string queueId) { }

        /// <inheritdoc />
        public void RecordError(string queueId, string exception) { }

        /// <inheritdoc />
        public void RecordRollback(string queueId) { }

        /// <inheritdoc />
        public void RecordDelete(string queueId) { }

        /// <inheritdoc />
        public void RecordExpire(string queueId) { }
    }
}
