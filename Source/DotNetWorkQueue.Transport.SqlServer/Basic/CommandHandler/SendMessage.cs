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
            SqlServerMessageQueueTransportOptions options)
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

            options.AddBuiltInColumnsParams(command, data);

            command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
            command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;

            //add configurable column command params - user
            if (!options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsParams(command, data);
            }
        }

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
            if (options.EnableDelayedProcessing && delay.HasValue && delay != TimeSpan.Zero) return false;
            if (options.EnableMessageExpiration && expiration != TimeSpan.Zero) return false;
            return true;
        }

        /// <summary>
        /// The parameters, which are added the same way whether the text was cached or built.
        /// </summary>
        private static void AddMetaParameters(SqlCommand command, IAdditionalMessageData data, long id,
            SqlServerMessageQueueTransportOptions options)
        {
            options.AddBuiltInColumnsParams(command, data);

            command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
            command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;

            //add configurable column command params - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsParams(command, data);
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
            TimeSpan expiration)
        {
            //The text is fixed for a queue unless the message carries a delay, an expiration or
            //user columns - and with the default options none of those apply, because
            //EnableDelayedProcessing and EnableMessageExpiration are both off. Rebuilding it per
            //send cost 4,986 bytes, 16% of everything a send allocated and 36% of what the library
            //added over a hand-written write of the same shape.
            //
            //Only the invariant shape is cached. A delay or an expiration is written into the SQL
            //as a literal - DATEADD(ms,12345,...) - so those texts differ per message and caching
            //them would be unbounded. Parameterising those two would let this cover every message
            //and would stop SQL Server compiling a fresh plan per distinct delay, which is worth
            //doing separately.
            var cacheKey = CanCacheMetaSql(data, options, delay, expiration)
                ? tableNameHelper.MetaDataName + "|" + options.GetMetaSqlShape()
                : null;

            if (cacheKey != null && MetaSqlCache.TryGetValue(cacheKey, out var cached))
            {
                command.CommandText = cached;
                AddMetaParameters(command, data, id, options);
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
            options.AddBuiltInColumnValues(delay, expiration, sbMeta);

            //add configurable column value - user
            if (options.AdditionalColumnsOnMetaData)
            {
                AddUserColumnsValues(sbMeta, data);
            }

            sbMeta.Append(')'); //close the VALUES

            command.CommandText = sbMeta.ToString();
            if (cacheKey != null)
            {
                MetaSqlCache.TryAdd(cacheKey, command.CommandText);
            }

            AddMetaParameters(command, data, id, options);

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
