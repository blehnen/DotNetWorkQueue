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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Schema
{
    /// <summary>
    /// Schema version 2: timestamp columns become timestamptz.
    /// </summary>
    /// <remarks>
    /// 0.12.0 changed this transport to store history and metadata timestamps as timestamptz, because
    /// a naive timestamp read back shifted by the machine's UTC offset - so EnqueuedUtc held local
    /// time and purging by date removed the wrong window (#311). Only queues created after it got the
    /// new columns; an older queue kept timestamp, and the only way to convert it was to run
    /// docs/upgrade/0.12.0/postgresql.sql by hand, editing the queue name into it.
    ///
    /// This is that conversion as a schema version. PostgreSQL only: no other transport changed type.
    ///
    /// COST. ALTER ... TYPE takes an ACCESS EXCLUSIVE lock and rewrites the table. Brief on MetaData,
    /// which holds only in-flight messages; History is as large as the retention on it. That is
    /// tolerable here in a way it is not in general, because a queue below its target version already
    /// refuses producers and consumers - so the upgrade runs with nothing attached to the queue rather
    /// than blocking live traffic.
    /// </remarks>
    public class PostgreSqlTimestampTzVersion : ISchemaVersion
    {
        private readonly IConnectionInformation _connectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlTimestampTzVersion"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information, which carries the declared source time zone.</param>
        public PostgreSqlTimestampTzVersion(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(connectionInformation);
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// The columns 0.12.0 changed, as (table, column) pairs.
        /// </summary>
        /// <remarks>
        /// MetaDataErrors carries QueuedDateTime because it is built by cloning the metadata table's
        /// columns, not just the two of its own.
        /// </remarks>
        private static IEnumerable<(string Table, string Column)> Columns(ITableNameHelper tableNames)
        {
            yield return (tableNames.MetaDataName, "QueuedDateTime");
            yield return (tableNames.MetaDataErrorsName, "QueuedDateTime");
            yield return (tableNames.MetaDataErrorsName, "LastExceptionDate");
            yield return (tableNames.HistoryName, "EnqueuedUtc");
            yield return (tableNames.HistoryName, "StartedUtc");
            yield return (tableNames.HistoryName, "CompletedUtc");
        }

        /// <inheritdoc />
        public string Script(ITableNameHelper tableNames, DbConnection connection, DbTransaction transaction)
        {
            Guard.NotNull(tableNames);
            Guard.NotNull(connection);
            Guard.NotNull(transaction);

            var toConvert = Columns(tableNames)
                .Where(x => NeedsConverting(x.Table, x.Column, connection, transaction))
                .ToList();

            var script = new StringBuilder();

            //Nothing to convert: the queue was created after 0.12.0, or the 0.12.0 script has already
            //been run against it, or the optional tables are not there. Say so in a statement rather
            //than returning nothing, because an empty script is treated as a version that failed to
            //produce one - and ask for no time zone, because none is needed to do nothing.
            if (toConvert.Count == 0)
            {
                script.AppendLine("SELECT 1; --nothing to convert: the columns are already timestamptz");
                return script.ToString();
            }

            //The conversion reads each naive value as being in the session's time zone. Before 0.12.0
            //the value stored was the local representation on the machine that wrote it, and nothing
            //in the database records which zone that was - so it is asked for rather than assumed.
            //Assuming UTC would silently shift every timestamp on any deployment that was not in UTC,
            //which is the defect #311 fixed, reintroduced by the fix for it.
            var timeZone = _connectionInformation.AdditionalConnectionSettings.GetUpgradeSourceTimeZone();
            if (string.IsNullOrWhiteSpace(timeZone))
            {
                throw new DotNetWorkQueueException(
                    $"{toConvert.Count} column(s) still hold a naive timestamp and have to be converted to " +
                    "timestamptz, but the time zone they were written in has not been declared. The value " +
                    "stored before 0.12.0 was the local time of the machine that wrote it, and nothing in " +
                    "the database records which zone that was, so converting without it would shift every " +
                    "timestamp. Call SetUpgradeSourceTimeZone on the connection's additional settings - " +
                    "\"UTC\" if the application ran in UTC - and upgrade again.");
            }

            //LOCAL, so it lasts for the upgrade transaction and does not leak into the connection
            script.AppendLine($"SET LOCAL TIME ZONE {Quote(timeZone)};");

            //One statement per table, not per column. ALTER ... TYPE rewrites the whole table and
            //holds an ACCESS EXCLUSIVE lock while it does, so converting History's three columns
            //separately would rewrite it three times and hold that lock three times over. Naming
            //them in one statement rewrites it once.
            foreach (var table in toConvert.GroupBy(x => x.Table))
            {
                var alters = string.Join(", ",
                    table.Select(x => $"ALTER COLUMN {x.Column} TYPE timestamptz"));
                script.AppendLine($"ALTER TABLE {table.Key} {alters};");
            }

            return script.ToString();
        }

        /// <summary>
        /// The time zone as a SQL string literal.
        /// </summary>
        /// <remarks>
        /// SET TIME ZONE takes a value rather than an identifier, but it is not a statement that
        /// accepts a parameter, so the name is quoted here. It comes from the host's own configuration
        /// rather than from a message, and doubling any quote in it keeps a stray one from ending the
        /// literal.
        /// </remarks>
        private static string Quote(string timeZone) => "'" + timeZone.Replace("'", "''") + "'";

        /// <summary>
        /// Whether the column is there and still a naive timestamp.
        /// </summary>
        /// <remarks>
        /// Two reasons this is a question rather than an assumption. The table may not exist at all -
        /// History is only created when that option is on, and it is off by default. And the column may
        /// already be timestamptz, because the queue was created after 0.12.0 or because the 0.12.0
        /// script was run against it by hand; converting one twice is an error, and it would take the
        /// whole upgrade with it.
        ///
        /// Read through to_regclass and pg_attribute rather than information_schema, for the reason the
        /// 0.12.0 script gives: information_schema matches a bare table name and can answer for a
        /// same-named table in another schema, and to_regclass resolves a name PostgreSQL stored
        /// truncated.
        /// </remarks>
        private static bool NeedsConverting(string table, string column, DbConnection connection,
            DbTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    @"SELECT 1 FROM pg_attribute a
                      WHERE a.attrelid = to_regclass(@Table)
                        AND a.attnum > 0 AND NOT a.attisdropped
                        AND lower(a.attname) = lower(@Column)
                        AND a.atttypid = 'timestamp without time zone'::regtype";

                var tableParameter = command.CreateParameter();
                tableParameter.ParameterName = "@Table";
                tableParameter.Value = table;
                command.Parameters.Add(tableParameter);

                var columnParameter = command.CreateParameter();
                columnParameter.ParameterName = "@Column";
                columnParameter.Value = column;
                command.Parameters.Add(columnParameter);

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}
