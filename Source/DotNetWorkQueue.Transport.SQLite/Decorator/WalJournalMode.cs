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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// Sets WAL journal mode on a newly created file-based database.
    /// </summary>
    /// <remarks>
    /// Shared by the two creation decorators rather than written out in each. Write-ahead logging is
    /// the only concurrency lever SQLite gives this transport - the engine has no asynchronous I/O
    /// and never will, see <see cref="IReaderAsync"/> - so a database that misses it is meaningfully
    /// worse under load, and silently: nothing converts an existing file later. A copy of this that
    /// one caller forgot is exactly how job databases ended up in rollback-journal mode.
    /// </remarks>
    internal static class WalJournalMode
    {
        /// <summary>
        /// Applies the journal mode to the database behind <paramref name="connectionInformation"/>,
        /// when the transport options ask for it and the database is a file.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getFileNameFromConnection">Resolves the file behind the connection string.</param>
        /// <param name="options">The transport options, which carry the opt-out.</param>
        public static void Apply(IConnectionInformation connectionInformation,
            IGetFileNameFromConnectionString getFileNameFromConnection,
            ISqLiteMessageQueueTransportOptionsFactory options)
        {
            if (!options.Create().EnableWalMode)
                return;

            //an in-memory database has no journal to write
            if (getFileNameFromConnection.GetFileName(connectionInformation.ConnectionString).IsInMemory)
                return;

            using (var connection = new SQLiteConnection(connectionInformation.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA journal_mode=WAL;";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
