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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    internal static class SendMessage
    {
        /// <summary>The CTE holding the body insert, whose <c>RETURNING</c> supplies the identity.</summary>
        private const string BodyCte = "b";

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildStatusCommand(NpgsqlCommand command,
            ITableNameHelper tableNameHelper,
            IAdditionalMessageData data,
            long id,
            PostgreSqlMessageQueueTransportOptions options,
            string queueIdFromCte = null,
            bool includeSharedParameters = true)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Insert into " + tableNameHelper.StatusName);
            builder.Append("(QueueID, Status, CorrelationID ");

            //add configurable columns - user
            if (!options.AdditionalColumnsOnMetaData)
            {
                AddUserColumns(builder, data);
            }

            //close the column list
            builder.AppendLine(") ");

            //add standard values that are always present. Inside a CTE the identity is a column
            //of the insert above rather than something the client knows, so the VALUES list
            //becomes a SELECT over it.
            if (queueIdFromCte == null)
            {
                builder.Append("VALUES (");
                builder.Append($"@QueueID, {Convert.ToInt32(QueueStatuses.Waiting)}, @CorrelationID");
            }
            else
            {
                builder.Append("SELECT ");
                builder.Append($"{queueIdFromCte}.QueueID, {Convert.ToInt32(QueueStatuses.Waiting)}, @CorrelationID");
            }

            //add configurable column value - user
            if (!options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsValues(builder, data);
            }

            builder.Append(queueIdFromCte == null ? ")" : $" FROM {queueIdFromCte}");

            command.CommandText = builder.ToString();

            //When composed into one statement the meta insert has already bound these, and one
            //parameter serves every occurrence of its name. @QueueID is a CTE column, not a
            //parameter at all. User columns are still bound here - when they live on the status
            //table the meta insert never binds them.
            if (includeSharedParameters)
            {
                options.AddBuiltInColumnsParams(command, data);

                command.Parameters.Add("@QueueID", NpgsqlDbType.Bigint, 8).Value = id;
                command.Parameters.Add("@CorrelationID", NpgsqlDbType.Uuid, 16).Value = data.CorrelationId.Id.Value;
            }

            //add configurable column command params - user
            if (!options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsParams(command, data);
            }
        }

        /// <summary>
        /// How many distinct statements either cache will hold. The key includes the queue's table
        /// names, and a queue name is whatever the caller chose, so an application that creates
        /// short-lived queues under generated names would otherwise add an entry per queue and
        /// never drop one - the benchmarks and the integration tests do exactly that. Past the cap
        /// nothing is evicted and nothing is added; a statement is simply rebuilt per send, which
        /// is what already happens for every shape that is not cacheable.
        /// </summary>
        private const int MaxCachedStatements = 500;

        /// <summary>Composed single-round-trip statements, keyed by table names and option shape.</summary>
        private static readonly ConcurrentDictionary<string, string> SingleRoundTripSqlCache = new();

        /// <summary>
        /// Whether this message's statement is the invariant shape and may be cached.
        /// </summary>
        /// <remarks>
        /// A delay or an expiration is written into the meta insert as a literal tick count, so
        /// those texts differ per message. <see cref="PostgreSqlMessageQueueTransportOptions.EnableDelayedProcessing"/>
        /// is stricter than it looks: with it on, the current time is inlined even when the message
        /// carries <b>no</b> delay, so every send produces a different statement. SQL Server writes
        /// an invariant <c>GetUTCDate()</c> in that position instead. Parameterising the two would
        /// let this cover every message and is worth doing separately - it is why a delayed-processing
        /// queue re-plans on every send.
        /// <para>
        /// User columns on the status table are written into that insert by name, so those are
        /// excluded too.
        /// </para>
        /// </remarks>
        private static bool CanCacheSingleRoundTripSql(IAdditionalMessageData data,
            PostgreSqlMessageQueueTransportOptions options, TimeSpan expiration)
        {
            if (options.AdditionalColumnsOnMetaData) return false;
            if (options.EnableDelayedProcessing) return false;
            if (options.EnableMessageExpiration && expiration != TimeSpan.Zero) return false;
            if (options.EnableStatusTable && data.AdditionalMetaData.Count > 0) return false;
            return true;
        }

        /// <summary>
        /// The whole of an ordinary send as one statement: the body insert, the meta insert, the
        /// status insert where that table is enabled, and the returned identity.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a single statement built from data-modifying CTEs, not a batch of several. That
        /// distinction is the reason there is no <c>BEGIN</c>/<c>COMMIT</c> here and no error
        /// handling to go with it: a single statement in PostgreSQL is atomic on its own, so a
        /// failure anywhere in it rolls the whole thing back. The equivalent SQL Server change had
        /// to add <c>TRY/CATCH</c> with an explicit rollback, because a batch can keep executing
        /// after an error that does not abort the transaction and reach its own <c>COMMIT</c>.
        /// </para>
        /// <para>
        /// The meta and status inserts are the same text the four-round-trip path uses, in their
        /// <c>SELECT ... FROM</c> form, so both paths write identical rows. Neither binds
        /// <c>@QueueID</c>: the identity is a column of the body insert's <c>RETURNING</c> rather
        /// than something the client can know.
        /// </para>
        /// <para>
        /// A data-modifying CTE runs whether or not the outer query references it, which is what
        /// makes the meta and status arms execute at all.
        /// </para>
        /// </remarks>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildSingleRoundTripCommand(NpgsqlCommand command,
            ITableNameHelper tableNameHelper,
            IAdditionalMessageData data,
            PostgreSqlMessageQueueTransportOptions options,
            TimeSpan? delay,
            TimeSpan expiration,
            DateTime currentDateTime)
        {
            //this also binds the meta parameters, minus @QueueID
            BuildMetaCommand(command, tableNameHelper, data, 0, options, delay,
                expiration, currentDateTime, queueIdFromCte: BodyCte);
            var metaSql = command.CommandText;

            //built before the cache is consulted, because this binds parameters as well as
            //producing text - a cache hit still needs them on the command
            string statusSql = null;
            if (options.EnableStatusTable)
            {
                using var statusCommand = new NpgsqlCommand();
                BuildStatusCommand(statusCommand, tableNameHelper, data, 0, options,
                    queueIdFromCte: BodyCte, includeSharedParameters: false);
                foreach (NpgsqlParameter statusParameter in statusCommand.Parameters)
                {
                    command.Parameters.Add(statusParameter.Clone());
                }
                statusSql = statusCommand.CommandText;
            }

            string cacheKey = null;
            if (CanCacheSingleRoundTripSql(data, options, expiration))
            {
                var statusPart = options.EnableStatusTable ? "|status" : string.Empty;
                cacheKey = tableNameHelper.QueueName + "|" + tableNameHelper.MetaDataName + "|" +
                           options.GetMetaSqlShape() + statusPart;
            }

            if (cacheKey != null && SingleRoundTripSqlCache.TryGetValue(cacheKey, out var cached))
            {
                command.CommandText = cached;
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"WITH {BodyCte} AS (");
            sb.AppendLine($"Insert into {tableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers) RETURNING QueueID");
            sb.AppendLine("), m AS (");
            sb.AppendLine(metaSql);
            sb.Append(')');
            if (statusSql != null)
            {
                sb.AppendLine(", s AS (");
                sb.AppendLine(statusSql);
                sb.Append(')');
            }
            sb.AppendLine();
            sb.Append($"SELECT QueueID FROM {BodyCte}");

            command.CommandText = sb.ToString();
            if (cacheKey != null)
            {
                if (SingleRoundTripSqlCache.Count < MaxCachedStatements)
            {
                SingleRoundTripSqlCache.TryAdd(cacheKey, command.CommandText);
            }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildMetaCommand(NpgsqlCommand command,
            ITableNameHelper tableNameHelper,
            IAdditionalMessageData data,
            long id,
            PostgreSqlMessageQueueTransportOptions options,
            TimeSpan? delay,
            TimeSpan expiration,
            DateTime currentDateTime,
            string queueIdFromCte = null)
        {
            var sbMeta = new StringBuilder();
            sbMeta.AppendLine("Insert into " + tableNameHelper.MetaDataName);
            sbMeta.Append("(QueueID, CorrelationID, QueuedDateTime ");

            //add configurable columns - queue
            options.AddBuiltInColumns(sbMeta);

            //add configurable columns - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumns(sbMeta, data);
            }

            //close the column list
            sbMeta.AppendLine(") ");

            //add standard values that are always present - see BuildStatusCommand for why a CTE
            //turns the VALUES list into a SELECT
            if (queueIdFromCte == null)
            {
                sbMeta.Append("VALUES (");
                sbMeta.Append("@QueueID, @CorrelationID, now() at time zone 'utc' ");
            }
            else
            {
                sbMeta.Append("SELECT ");
                sbMeta.Append($"{queueIdFromCte}.QueueID, @CorrelationID, now() at time zone 'utc' ");
            }

            //add the values for built in fields
            options.AddBuiltInColumnValues(delay, expiration, currentDateTime, sbMeta);

            //add configurable column value - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsValues(sbMeta, data);
            }

            sbMeta.Append(queueIdFromCte == null ? ")" : $" FROM {queueIdFromCte}");

            command.CommandText = sbMeta.ToString();

            options.AddBuiltInColumnsParams(command, data);

            if (queueIdFromCte == null)
            {
                command.Parameters.Add("@QueueID", NpgsqlDbType.Bigint, 8).Value = id;
            }
            command.Parameters.Add("@CorrelationID", NpgsqlDbType.Uuid, 16).Value = data.CorrelationId.Id.Value;

            //add configurable column command params - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsParams(command, data);
            }

        }
        /// <summary>
        /// Adds the SQL command params for the user specific meta data
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsParams(NpgsqlCommand command, IAdditionalMessageData data)
        {
            foreach (var metadata in data.AdditionalMetaData)
            {
                command.Parameters.AddWithValue("@" + metadata.Name, metadata.Value);
            }
        }

        /// <summary>
        /// Adds the user specific columns to the meta data SQL command string
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumns(StringBuilder command, IAdditionalMessageData data)
        {
            var i = 0;
            foreach (var metadata in data.AdditionalMetaData)
            {
                if (i == 0)
                {
                    command.Append(',');
                }
                command.Append(metadata.Name);
                if (i < data.AdditionalMetaData.Count - 1)
                {
                    command.Append(',');
                }
                i++;
            }
        }
        /// <summary>
        /// Adds the values for the user specific meta data to the SQL command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsValues(StringBuilder command, IAdditionalMessageData data)
        {
            var i = 0;
            foreach (var metadata in data.AdditionalMetaData)
            {
                if (i == 0)
                {
                    command.Append(',');
                }
                command.Append("@" + metadata.Name);
                if (i < data.AdditionalMetaData.Count - 1)
                {
                    command.Append(',');
                }
                i++;
            }
        }
    }
}
