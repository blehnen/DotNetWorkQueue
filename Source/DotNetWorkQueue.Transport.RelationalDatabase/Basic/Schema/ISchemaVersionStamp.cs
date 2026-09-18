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
using System.Data.Common;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Records a freshly created queue as being at the current schema version.
    /// </summary>
    /// <remarks>
    /// A new queue is created at the latest shape, so it must not then be told it needs upgrading. With
    /// no stamp it would have no version table, read as version zero, and every producer and consumer
    /// would be refused for a queue that was in fact perfectly current.
    ///
    /// Separate from <see cref="IQueueSchemaVersion"/> and not part of the public surface, because
    /// marking a queue current is only ever correct immediately after building it. Called against an
    /// old queue it would assert something untrue and skip the migrations that queue needs, which is
    /// the one way to lose data here that the framework otherwise makes impossible.
    /// </remarks>
    public interface ISchemaVersionStamp
    {
        /// <summary>
        /// Writes the current version, in the caller's transaction.
        /// </summary>
        /// <param name="connection">The connection the queue is being created on.</param>
        /// <param name="transaction">The transaction the queue is being created in.</param>
        /// <remarks>
        /// Takes the caller's transaction so the stamp commits with the tables it describes. A queue
        /// that exists without its version, or a version without its queue, is a state nothing else
        /// here knows how to read.
        ///
        /// Does nothing when the transport has no versions, so it costs a new queue nothing until
        /// there is something to record.
        /// </remarks>
        void MarkCurrent(DbConnection connection, DbTransaction transaction);
    }
}
