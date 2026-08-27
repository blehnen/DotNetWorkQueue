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
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    /// <summary>
    /// Builds the multi-row body insert for a batched send. The body table has uniform columns
    /// (Body, Headers) for every message, so it is safe to insert as a single statement; the
    /// dependent meta and (optional) status rows are inserted per-message by the handler because
    /// their columns can vary per message (user metadata).
    /// </summary>
    internal static class SendMessageBatch
    {
        /// <summary>
        /// A conservative bound-parameter ceiling. The bundled SQLite engine actually allows
        /// 32766 parameters (<c>SQLITE_MAX_VARIABLE_NUMBER</c>, SQLite ≥ 3.32), but a small,
        /// build-independent ceiling keeps the chunk size safe even on an interop compiled with
        /// the historical default of 999, and still yields a large multi-row insert.
        /// </summary>
        internal const int SqliteSafeMaxParameters = 999;

        /// <summary>
        /// Parameters bound per message in the multi-row body insert (@Body, @Headers).
        /// </summary>
        internal const int BodyParametersPerMessage = 2;

        /// <summary>
        /// The safe maximum number of messages per body-insert chunk, leaving headroom below the
        /// conservative parameter ceiling. Only the body insert is multi-row, so this single
        /// value governs chunking for the whole batch.
        /// </summary>
        internal static int SafeMaxBatchSize { get; } =
            (SqliteSafeMaxParameters - 99) / BodyParametersPerMessage;

        /// <summary>
        /// Builds a multi-row <c>INSERT … RETURNING QueueID</c> that inserts every body row in one
        /// statement and returns the generated ids. SQLite does not specify the row order of
        /// <c>RETURNING</c>, and (unlike SQL Server <c>MERGE … OUTPUT</c>) it cannot emit a
        /// source-only ordinal, so the handler recovers input order by sorting the returned ids
        /// ascending: <c>QueueID</c> is <c>INTEGER PRIMARY KEY AUTOINCREMENT</c>, so within this
        /// single insert (and single write transaction) the ids are assigned monotonically in
        /// VALUES order and the ascending sort reproduces the input order exactly.
        /// </summary>
        /// <param name="command">The command to populate.</param>
        /// <param name="tableNameHelper">Supplies the queue (body) table name.</param>
        /// <param name="rows">The serialized body and header bytes per message, in input order.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table name is from configuration; values are parameterized")]
        internal static void BuildBodyInsertReturningCommand(IDbCommand command,
            ITableNameHelper tableNameHelper,
            IReadOnlyList<(byte[] Body, byte[] Headers)> rows)
        {
            var builder = new StringBuilder();
            builder.Append("Insert into ").Append(tableNameHelper.QueueName).Append(" (Body, Headers) VALUES ");
            for (var i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append("(@Body").Append(i).Append(",@Headers").Append(i).Append(')');
            }
            builder.Append(" RETURNING QueueID;");

            command.CommandText = builder.ToString();

            for (var i = 0; i < rows.Count; i++)
            {
                var body = command.CreateParameter();
                body.ParameterName = "@Body" + i;
                body.DbType = DbType.Binary;
                body.Value = rows[i].Body;
                command.Parameters.Add(body);

                var headers = command.CreateParameter();
                headers.ParameterName = "@Headers" + i;
                headers.DbType = DbType.Binary;
                headers.Value = rows[i].Headers;
                command.Parameters.Add(headers);
            }
        }
    }
}
