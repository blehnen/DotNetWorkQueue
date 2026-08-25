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
using DotNetWorkQueue.Transport.SQLite;
using DotNetWorkQueue.Transport.SQLite.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Determines the full path and file name of a Sqlite DB, based on the connection string.
    /// </summary>
    public class GetFileNameFromConnectionString : IGetFileNameFromConnectionString
    {
        /// <summary>
        /// Parsing means constructing a <see cref="SQLiteConnectionStringBuilder"/>, which measured
        /// 7.2 us and 20.2 KB on net10 - a third of everything a send allocates. A send parses twice,
        /// once for the database existence check and once when a connection is created, and the
        /// receive path parses again per operation. Connection strings are stable for the life of a
        /// queue, and <see cref="ConnectionStringInfo"/> is immutable, so the result is cached and
        /// the instance shared.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConnectionStringInfo> Parsed =
            new ConcurrentDictionary<string, ConnectionStringInfo>(StringComparer.Ordinal);

        /// <summary>
        /// A process normally sees a handful of connection strings, but this cache is static and
        /// lives for the life of the process, so a caller that generates them dynamically - one per
        /// tenant, say - must not be able to grow it without bound. Past the cap the value is still
        /// returned, just parsed each time.
        /// </summary>
        private const int MaxCachedConnectionStrings = 128;

        /// <summary>Guards admission to <see cref="Parsed"/> so the cap above cannot be exceeded.</summary>
        private static readonly object Admission = new object();

        /// <summary>
        /// Gets the full path and file name of a DB. In memory databases will instead set the <seealso cref="ConnectionStringInfo.IsInMemory"/> flag to true.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public ConnectionStringInfo GetFileName(string connectionString)
        {
            //A null or empty string cannot be a dictionary key, and there is nothing to save by
            //caching the answer for one anyway.
            if (string.IsNullOrEmpty(connectionString))
                return Parse(connectionString);

            if (Parsed.TryGetValue(connectionString, out var cached))
                return cached;

            var result = Parse(connectionString);

            //Admission is serialised so the cap is an actual bound rather than an approximate one.
            //This runs only when the lookup above missed, which after warm-up is never, so it costs
            //nothing on the path that matters.
            lock (Admission)
            {
                if (Parsed.Count < MaxCachedConnectionStrings)
                    Parsed.TryAdd(connectionString, result);
            }

            return result;
        }

        private static ConnectionStringInfo Parse(string connectionString)
        {
            SQLiteConnectionStringBuilder builder;
            try
            {
                builder = new SQLiteConnectionStringBuilder(connectionString);
            }
            // ReSharper disable once UncatchableException
            catch (ArgumentException) //bad format - return a connection string info that isn't valid
            {
                return new ConnectionStringInfo(false, string.Empty);
            }

            var dataSource = builder.DataSource.ToLowerInvariant();
            var inMemory = dataSource.Contains(":memory:") || dataSource.Contains("mode=memory");

            if (inMemory || string.IsNullOrWhiteSpace(builder.ConnectionString))
                return new ConnectionStringInfo(inMemory, builder.DataSource);

            var uri = builder.ConnectionString.ToLowerInvariant();
            inMemory = uri.Contains(":memory:") || uri.Contains("mode=memory");

            return new ConnectionStringInfo(inMemory, builder.DataSource);
        }
    }
}
