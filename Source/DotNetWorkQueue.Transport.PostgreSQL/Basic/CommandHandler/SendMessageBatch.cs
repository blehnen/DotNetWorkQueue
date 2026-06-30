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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using Npgsql;
using NpgsqlTypes;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler
{
    /// <summary>
    /// Builds the multi-row body insert for a batched send. The body table has uniform columns
    /// (Body, Headers) for every message, so it is inserted as a single statement; the dependent
    /// meta and (optional) status rows are inserted per-message by the handler because their
    /// columns can vary per message (user metadata).
    /// </summary>
    internal static class SendMessageBatch
    {
        /// <summary>
        /// The safe maximum number of messages per chunk. The body insert binds the whole chunk
        /// as two array parameters (<c>unnest</c>), so it is NOT limited by the bound-parameter
        /// ceiling (65535 in Npgsql); this value instead bounds the size, lock duration and memory
        /// of a single batch transaction (the per-message meta/status rows are still issued one
        /// statement each within that transaction).
        /// </summary>
        internal const int SafeMaxBatchSize = 5000;

        /// <summary>
        /// Builds a multi-row <c>INSERT … SELECT … FROM unnest(@Bodies, @Headers) WITH ORDINALITY …
        /// ORDER BY ord RETURNING queueid</c>. The two <c>bytea[]</c> array parameters carry the
        /// whole chunk, so the statement is legal at any chunk size. The handler recovers input
        /// order by sorting the returned ids ascending: <c>queueid</c> is <c>bigserial</c> and the
        /// <c>ORDER BY ord</c> makes the sequence assigned in input order, so the assigned ids are
        /// monotonic in input order. Concurrent writers may interleave the sequence (gaps) but
        /// cannot reorder this batch's rows. PostgreSQL <c>RETURNING</c> exposes only the target
        /// table's columns, so it cannot emit the source ordinal directly (hence the sort).
        /// </summary>
        /// <param name="command">The command to populate.</param>
        /// <param name="tableNameHelper">Supplies the queue (body) table name.</param>
        /// <param name="rows">The serialized body and header bytes per message, in input order.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table name is from configuration; values are parameterized")]
        internal static void BuildBodyInsertReturningCommand(NpgsqlCommand command,
            ITableNameHelper tableNameHelper,
            IReadOnlyList<(byte[] Body, byte[] Headers)> rows)
        {
            var bodies = new byte[rows.Count][];
            var headers = new byte[rows.Count][];
            for (var i = 0; i < rows.Count; i++)
            {
                bodies[i] = rows[i].Body;
                headers[i] = rows[i].Headers;
            }

            command.CommandText =
                $"INSERT INTO {tableNameHelper.QueueName} (Body, Headers) " +
                "SELECT b, h FROM unnest(@Bodies, @Headers) WITH ORDINALITY AS t(b, h, ord) " +
                "ORDER BY ord RETURNING queueid;";

            command.Parameters.Add(new NpgsqlParameter("Bodies", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = bodies });
            command.Parameters.Add(new NpgsqlParameter("Headers", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = headers });
        }
    }
}
