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
using System.Collections.Concurrent;
using System.Data.SQLite;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Enables ADO.NET connection pooling on SQLite connection strings that do not already
    /// specify it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>System.Data.SQLite</c> defaults <c>Pooling</c> to <c>false</c>, unlike
    /// <c>Microsoft.Data.SqlClient</c> and <c>Npgsql</c>, which pool by default. The transport
    /// opens a connection per operation, which is the correct and idiomatic pattern for the
    /// pooled providers but is expensive here.
    /// </para>
    /// <para>
    /// The cost is not the open itself. In WAL journal mode — the transport default, see
    /// <see cref="SqLiteMessageQueueTransportOptions.EnableWalMode"/> — SQLite checkpoints the
    /// write-ahead log back into the database when the <em>last</em> connection to it closes.
    /// Opening and closing a connection per send therefore pays a checkpoint per message.
    /// Measured on net10 with a single-threaded producer, enabling pooling took a send from
    /// ~8.5 ms to ~1.0 ms. The same change against a database in rollback-journal mode made no
    /// difference at all (5.87 ms vs 5.86 ms), which is what identifies the close-checkpoint —
    /// rather than connection-open cost — as what pooling avoids here.
    /// </para>
    /// <para>
    /// Pooled connections keep the database file handle open. Callers that delete the database
    /// file must call <see cref="SQLiteConnection.ClearAllPools"/> first;
    /// <see cref="SqLiteMessageQueueCreation.RemoveQueue"/> does so on the caller's behalf.
    /// </para>
    /// </remarks>
    internal static class ConnectionStringPooling
    {
        private const string PoolingKeyword = "Pooling";

        private static readonly IGetFileNameFromConnectionString FileNameParser =
            new GetFileNameFromConnectionString();

        /// <summary>
        /// Connection strings are stable for the life of a queue and this sits on the send and
        /// receive paths, so the rewritten string is cached rather than re-parsed per operation.
        /// Rebuilding costs roughly 13 microseconds, about 1.4% of a send.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> Rewritten =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// A process normally sees a handful of connection strings, but this cache is static and
        /// lives for the life of the process, so a caller that generates them dynamically — one
        /// per tenant, say — must not be able to grow it without bound. Past the cap the value is
        /// still returned, just rebuilt each time.
        /// </summary>
        private const int MaxCachedConnectionStrings = 128;

        /// <summary>Guards admission to <see cref="Rewritten"/> so the cap above cannot be exceeded.</summary>
        private static readonly object Admission = new object();

        /// <summary>
        /// Returns <paramref name="connectionString"/> with pooling enabled, unless the caller
        /// already expressed a preference, the database is in-memory, or the connection is being
        /// created to hold an in-memory database open.
        /// </summary>
        /// <param name="connectionString">The connection string supplied by the caller.</param>
        /// <param name="forMemoryHold">
        /// True when the connection exists only to keep an in-memory database alive. Such a
        /// connection is never released, so pooling it would serve no purpose.
        /// </param>
        internal static string Apply(string connectionString, bool forMemoryHold)
        {
            if (forMemoryHold || string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            if (Rewritten.TryGetValue(connectionString, out var cached))
                return cached;

            var result = Build(connectionString);

            //Admission is serialised so the cap is an actual bound rather than an approximate one.
            //This runs only when the lookup above missed, which after warm-up is never, so it costs
            //nothing on the path that matters.
            lock (Admission)
            {
                if (Rewritten.Count < MaxCachedConnectionStrings)
                    Rewritten.TryAdd(connectionString, result);
            }

            return result;
        }

        private static string Build(string connectionString)
        {
            SQLiteConnectionStringBuilder builder;
            try
            {
                builder = new SQLiteConnectionStringBuilder(connectionString);
            }
            // ReSharper disable once UncatchableException
            catch (ArgumentException) //malformed - leave it exactly as the caller supplied it
            {
                return connectionString;
            }

            //an explicit Pooling=true or Pooling=false is the caller's decision; do not override it
            if (builder.ContainsKey(PoolingKeyword))
                return connectionString;

            //an in-memory database is kept alive by SqLiteHoldConnection, and with shared cache a
            //pooled connection would keep it alive past the point the caller disposed of it
            if (FileNameParser.GetFileName(connectionString).IsInMemory)
                return connectionString;

            //Append rather than returning builder.ConnectionString. The builder is used only to
            //inspect: round-tripping through it rewrites the caller's string, and for input it
            //cannot make sense of it discards the content entirely - "this is not a connection
            //string=;;;" comes back as "pooling=True". Appending keeps whatever the caller passed
            //exactly as they passed it, so a connection string we misjudge still fails the way it
            //would have without us.
            var trimmed = connectionString.TrimEnd();
            //EndsWith(char) is netstandard2.1+; this line targets net461 upward
            var separator = trimmed.EndsWith(";", StringComparison.Ordinal) ? string.Empty : ";";
            return trimmed + separator + PoolingKeyword + "=True;";
        }
    }
}
