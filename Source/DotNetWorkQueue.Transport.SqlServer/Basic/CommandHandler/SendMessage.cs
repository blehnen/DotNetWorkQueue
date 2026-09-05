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
using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    internal static class SendMessage
    {
        internal static void BuildStatusCommand(SqlCommand command,
            ITableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            long id,
            SqlServerMessageQueueTransportOptions options,
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

            //add standard values that are always present
            builder.Append("VALUES (");
            builder.Append($"@QueueID, {Convert.ToInt32(QueueStatuses.Waiting)}, @CorrelationID");

            //add configurable column value - user
            if (!options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsValues(builder, data);
            }

            builder.Append(')'); //close the VALUES

            command.CommandText = builder.ToString();

            //When composed into a single batch the meta insert has already bound these, and one
            //SqlParameter serves every occurrence of its name in the text. @QueueID is a variable
            //the batch declares rather than a parameter at all. The user columns are still added
            //here, because when they live on the status table the meta insert never binds them.
            if (includeSharedParameters)
            {
                options.AddBuiltInColumnsParams(command, data);

                command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
                command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;
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

        /// <summary>
        /// Meta-insert SQL, keyed by table name and the option shape that produced it. Bounded by
        /// the number of queues times the option combinations in use, and only ever holds the
        /// shape that carries no per-message literals.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> MetaSqlCache = new();

        /// <summary>
        /// Whether this message's meta SQL is the invariant shape. Anything that writes a literal
        /// into the text - a delay, an expiration - or that varies with the message's own columns
        /// is built fresh.
        /// </summary>
        private static bool CanCacheMetaSql(IAdditionalMessageData data,
            SqlServerMessageQueueTransportOptions options, TimeSpan? delay, TimeSpan expiration)
        {
            if (options.AdditionalColumnsOnMetaData) return false;
            return true;
        }

        /// <summary>
        /// The parameters, which are added the same way whether the text was cached or built.
        /// </summary>
        private static void AddMetaParameters(SqlCommand command, IAdditionalMessageData data, long id,
            SqlServerMessageQueueTransportOptions options, bool includeQueueId, TimeSpan? delay,
            TimeSpan expiration)
        {
            options.AddBuiltInTimeParams(command, delay, expiration);
            options.AddBuiltInColumnsParams(command, data);

            //When the caller is composing this into a single batch, @QueueID is a variable the
            //batch declares and fills from SCOPE_IDENTITY, not something the client can know yet.
            //A parameter of the same name would collide with that declaration.
            if (includeQueueId)
            {
                command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
            }
            command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;

            //add configurable column command params - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsParams(command, data);
            }
        }

        /// <summary>Composed single-round-trip batches, keyed like <see cref="MetaSqlCache"/>.</summary>
        private static readonly ConcurrentDictionary<string, string> SingleRoundTripSqlCache = new();

        /// <summary>
        /// The whole of an ordinary send as one batch: the body insert, the identity, the meta
        /// insert and the transaction, with nothing returning to the client in between.
        /// </summary>
        /// <remarks>
        /// The meta statement is the same text the four-round-trip path uses, so both write
        /// identical rows. It is built without its <c>@QueueID</c> parameter because the batch
        /// declares a variable of that name and fills it from <c>SCOPE_IDENTITY</c> - a parameter
        /// of the same name would collide with the declaration.
        /// <para>
        /// The <c>TRY/CATCH</c> with an explicit <c>ROLLBACK</c> is what makes this all-or-nothing,
        /// and <c>XACT_ABORT</c> alone is <b>not</b> enough to get there. <c>SET XACT_ABORT ON</c>
        /// has no effect on errors raised by <c>RAISERROR</c>, so a trigger on the meta table that
        /// raises one leaves the transaction alive, execution reaches the unconditional
        /// <c>COMMIT</c>, and a body row is committed while the caller is told the send failed -
        /// exactly the orphan the client-side transaction it replaces could not produce. Both are
        /// kept: <c>XACT_ABORT</c> dooms the transaction on ordinary run-time errors, and the
        /// <c>CATCH</c> covers what it does not. <c>THROW</c> re-raises so the caller still sees
        /// the original error.
        /// </para>
        /// <para>
        /// Cached on the same terms as the meta SQL, and for the same reason: a message carrying a
        /// delay or an expiration has those written into the text as literals, so its batch differs
        /// per message and must not be cached.
        /// </para>
        /// </remarks>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildSingleRoundTripCommand(SqlCommand command,
            ITableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            SqlServerMessageQueueTransportOptions options,
            TimeSpan? delay,
            TimeSpan expiration)
        {
            //this also adds the meta parameters to the command
            BuildMetaCommand(command, tableNameHelper, headers, data, message, 0, options, delay,
                expiration, includeQueueIdParameter: false);
            var metaSql = command.CommandText;

            //Built before the cache is consulted, because this binds parameters as well as
            //producing text - a cache hit still needs them on the command.
            string statusSql = null;
            if (options.EnableStatusTable)
            {
                using var statusCommand = new SqlCommand();
                BuildStatusCommand(statusCommand, tableNameHelper, headers, data, message, 0, options,
                    includeSharedParameters: false);
                foreach (SqlParameter statusParameter in statusCommand.Parameters)
                {
                    command.Parameters.Add(((ICloneable)statusParameter).Clone());
                }
                statusSql = statusCommand.CommandText;
            }

            //The status insert carries the user's own columns when they live on the status table,
            //so its text varies per message and the batch must not be cached then. The meta insert
            //has no equivalent case - CanCacheMetaSql already refuses AdditionalColumnsOnMetaData.
            var statusEmbedsUserColumns = options.EnableStatusTable && data.AdditionalMetaData.Count > 0;

            //every table the statement writes to is named in the key. Both shipped helpers derive
            //StatusName from QueueName, but ITableNameHelper exposes the two independently -
            //keying on the derivation rather than the name would serve one helper's SQL to
            //another whose status table is somewhere else.
            var cacheKey = CanCacheMetaSql(data, options, delay, expiration) && !statusEmbedsUserColumns
                ? tableNameHelper.QueueName + "|" + tableNameHelper.MetaDataName + "|" +
                  options.GetMetaSqlShape() +
                  (options.EnableStatusTable ? "|" + tableNameHelper.StatusName : string.Empty)
                : null;

            if (cacheKey != null && SingleRoundTripSqlCache.TryGetValue(cacheKey, out var cached))
            {
                command.CommandText = cached;
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("SET NOCOUNT ON;");
            sb.AppendLine("SET XACT_ABORT ON;");
            sb.AppendLine("DECLARE @QueueID bigint;");
            sb.AppendLine("BEGIN TRY");
            sb.AppendLine("BEGIN TRANSACTION;");
            sb.AppendLine($"Insert into {tableNameHelper.QueueName} (Body, Headers) VALUES (@Body, @Headers);");
            sb.AppendLine("SET @QueueID = SCOPE_IDENTITY();");
            sb.Append(metaSql).AppendLine(";");
            if (statusSql != null)
            {
                sb.Append(statusSql).AppendLine(";");
            }
            sb.AppendLine("COMMIT TRANSACTION;");
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;");
            sb.AppendLine("THROW;");
            sb.AppendLine("END CATCH");
            sb.AppendLine("SELECT @QueueID;");

            command.CommandText = sb.ToString();
            if (cacheKey != null && SingleRoundTripSqlCache.Count < MaxCachedStatements)
            {
                SingleRoundTripSqlCache.TryAdd(cacheKey, command.CommandText);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildMetaCommand(SqlCommand command,
            ITableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            long id,
            SqlServerMessageQueueTransportOptions options,
            TimeSpan? delay,
            TimeSpan expiration,
            bool includeQueueIdParameter = true)
        {
            //The text is fixed for a queue unless the message carries a delay, an expiration or
            //user columns - and with the default options none of those apply, because
            //EnableDelayedProcessing and EnableMessageExpiration are both off. Rebuilding it per
            //send cost 4,986 bytes, 16% of everything a send allocated and 36% of what the library
            //added over a hand-written write of the same shape.
            //
            //The delay and the expiration ride as parameters rather than literals, so the text
            //no longer varies per message and this covers every send rather than only the
            //invariant shape - which also stops SQL Server compiling a fresh plan per distinct
            //delay value.
            var cacheKey = CanCacheMetaSql(data, options, delay, expiration)
                ? tableNameHelper.MetaDataName + "|" + options.GetMetaSqlShape()
                : null;

            if (cacheKey != null && MetaSqlCache.TryGetValue(cacheKey, out var cached))
            {
                command.CommandText = cached;
                AddMetaParameters(command, data, id, options, includeQueueIdParameter, delay, expiration);
                return;
            }

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

            //add standard values that are always present
            sbMeta.Append("VALUES (");
            sbMeta.Append("@QueueID, @CorrelationID, GetUTCDate() ");

            //add the values for built in fields
            options.AddBuiltInColumnValues(sbMeta);

            //add configurable column value - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsValues(sbMeta, data);
            }

            sbMeta.Append(')'); //close the VALUES

            command.CommandText = sbMeta.ToString();
            if (cacheKey != null && MetaSqlCache.Count < MaxCachedStatements)
            {
                MetaSqlCache.TryAdd(cacheKey, command.CommandText);
            }

            AddMetaParameters(command, data, id, options, includeQueueIdParameter, delay, expiration);
        }
        /// <summary>
        /// Adds the SQL command params for the user specific meta data
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsParams(SqlCommand command, IAdditionalMessageData data)
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
