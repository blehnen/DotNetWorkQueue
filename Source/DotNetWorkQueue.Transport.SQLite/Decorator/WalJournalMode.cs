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
using System;
using System.Data.SQLite;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.Extensions.Logging;

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
        private const string Wal = "wal";

        /// <summary>
        /// The journal mode to switch to, and the wait for the exclusive lock it needs.
        /// </summary>
        /// <remarks>
        /// Converting to WAL takes the database to itself for an instant. On a database this call
        /// has just created there is nothing to wait for; on one that is already in use there may
        /// be, which is what the busy timeout covers. Three seconds is generous for a one-time
        /// creation and still bounded, so a queue cannot hang on a lock somebody else is holding.
        /// </remarks>
        private const string BusyTimeout = "PRAGMA busy_timeout=3000;";

        /// <summary>
        /// The conversion itself. Its result is the journal mode the database is in afterwards, which
        /// is not necessarily the one asked for - see <see cref="Apply"/>.
        /// </summary>
        private const string SetWalMode = "PRAGMA journal_mode=WAL;";

        /// <summary>
        /// Applies the journal mode to the database behind <paramref name="connectionInformation"/>,
        /// when the transport options ask for it and the database is a file.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getFileNameFromConnection">Resolves the file behind the connection string.</param>
        /// <param name="options">The transport options, which carry the opt-out.</param>
        /// <param name="logger">Records a mode that could not be set.</param>
        /// <remarks>
        /// This never throws, and that is the whole point of it. The journal mode is a performance
        /// setting applied on top of the tables the caller actually asked for, and SQLite refuses the
        /// conversion while any other connection holds the database - "database is locked". Letting
        /// that escape would fail a creation whose schema had already committed, and the caller's
        /// retry would then find the tables present, return
        /// <see cref="QueueCreationStatus.AttemptedToCreateAlreadyExists"/> and never reach this code
        /// again, leaving the database in rollback-journal mode permanently. A warning is the right
        /// outcome: the queue works, it is just not in the mode that was asked for.
        /// </remarks>
        public static void Apply(IConnectionInformation connectionInformation,
            IGetFileNameFromConnectionString getFileNameFromConnection,
            ISqLiteMessageQueueTransportOptionsFactory options,
            ILogger logger)
        {
            if (!options.Create().EnableWalMode)
                return;

            //an in-memory database has no journal to write
            var fileName = getFileNameFromConnection.GetFileName(connectionInformation.ConnectionString);
            if (fileName.IsInMemory)
                return;

            try
            {
                var mode = Set(connectionInformation.ConnectionString);
                if (string.Equals(mode, Wal, StringComparison.OrdinalIgnoreCase))
                    return;

                //a refused conversion is not an error to SQLite; it reports it by handing back the
                //mode it decided to keep
                logger.LogWarning(
                    "Could not set WAL journal mode on {FileName}; the database is in {JournalMode} mode instead. It will work, with less concurrency than WAL allows",
                    fileName.FileName, mode ?? "an unknown");
            }
            catch (SQLiteException error)
            {
                logger.LogWarning(error,
                    "Could not set WAL journal mode on {FileName}, which is usually another connection holding the database. It will work, with less concurrency than WAL allows",
                    fileName.FileName);
            }
        }

        /// <summary>
        /// Runs the pragma and returns the journal mode the database ended up in.
        /// </summary>
        private static string Set(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                //separate commands on purpose: the busy timeout pragma returns a row of its own, so
                //running both at once would read that back rather than the journal mode
                using (var busy = connection.CreateCommand())
                {
                    busy.CommandText = BusyTimeout;
                    busy.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = SetWalMode;
                    return command.ExecuteScalar() as string;
                }
            }
        }
    }
}
