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
namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Determines the size of a relational batch-send chunk. A batch larger than the
    /// returned size is split into multiple multi-row inserts, all run inside one
    /// transaction.
    /// </summary>
    /// <remarks>
    /// Relational counterpart to the Redis transport's batch-size abstraction. The default
    /// implementation clamps a user-requested ceiling down to a transport-computed safe
    /// maximum (derived from the database's command parameter limit) so a configured value
    /// can never overflow the parameter budget.
    /// </remarks>
    public interface ISendBatchSize
    {
        /// <summary>
        /// Returns the number of messages to place in a single multi-row insert, given the
        /// total number of messages being sent.
        /// </summary>
        /// <param name="messageCount">The total number of messages in the batch.</param>
        /// <returns>The chunk size; never larger than <paramref name="messageCount"/> and
        /// never larger than the transport safe maximum.</returns>
        int BatchSize(int messageCount);
    }
}
