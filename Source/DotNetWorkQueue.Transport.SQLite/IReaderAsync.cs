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
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.SQLite
{
    /// <summary>
    /// Executes a <see cref="DbCommand"/> through the provider's async members, so the shared async
    /// handler path has something to await on SQLite.
    /// </summary>
    /// <remarks>
    /// <para>Awaiting these does not yield. SQLite has no asynchronous I/O, so
    /// <c>System.Data.SQLite</c> overrides none of <c>OpenAsync</c>, <c>ExecuteNonQueryAsync</c>,
    /// <c>ExecuteReaderAsync</c> or <c>ReadAsync</c>; the base-class implementations run the
    /// synchronous work on the calling thread and hand back a completed task. Every await on the
    /// SQLite async path therefore completes synchronously.</para>
    /// <para>Treat this as permanent rather than as a gap waiting to be filled: the SQLite project
    /// does not intend to add asynchronous I/O to the engine, and offers write-ahead logging as the
    /// answer to the concurrency problem async would have solved. This transport enables WAL by
    /// default; see <c>SQLiteMessageQueueTransportOptions.EnableWalMode</c>.</para>
    /// <para>Changing providers does not lift this either. <c>Microsoft.Data.Sqlite</c> overrides only
    /// <c>ExecuteReaderAsync</c> and <c>ExecuteDbDataReaderAsync</c>, and documents that those also
    /// execute synchronously: the limitation belongs to SQLite itself, not to the driver. Wrapping
    /// the calls in <c>Task.Run</c> would move a microsecond-scale local file read onto a pool
    /// thread, which costs more than it saves. GitHub issue #298.</para>
    /// </remarks>
    public interface IReaderAsync
    {
        /// <summary>
        /// Executes the non query asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<int> ExecuteNonQueryAsync(DbCommand command);
        /// <summary>
        /// Executes a scalar method asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<object> ExecuteScalarAsync(DbCommand command);
        /// <summary>
        /// Executes the reader asynchronous.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task<IDataReader> ExecuteReaderAsync(DbCommand command);
    }
}
