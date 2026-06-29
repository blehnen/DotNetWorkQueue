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
using Microsoft.Data.SqlClient;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
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
        /// The largest number of bound parameters a single SQL Server command accepts.
        /// </summary>
        internal const int SqlServerMaxParameters = 2100;

        /// <summary>
        /// Parameters bound per message in the multi-row body insert (@Body, @Headers). The
        /// row ordinal is emitted as an inline integer literal, not a parameter.
        /// </summary>
        internal const int BodyParametersPerMessage = 2;

        /// <summary>
        /// The safe maximum number of messages per body-insert chunk, leaving headroom below the
        /// SQL Server parameter limit. Only the body insert is multi-row, so this single value
        /// governs chunking for the whole batch.
        /// </summary>
        internal static int SafeMaxBatchSize { get; } =
            (SqlServerMaxParameters - 100) / BodyParametersPerMessage;

        /// <summary>
        /// Builds a <c>MERGE … OUTPUT</c> that inserts every body row in one statement and
        /// returns the generated <c>QueueID</c> alongside the caller-supplied ordinal, so the
        /// handler can re-associate ids with input order (SQL Server does not guarantee
        /// identity-assignment order matches the VALUES order). <c>MERGE</c> is required because
        /// a plain <c>INSERT … OUTPUT</c> cannot emit a source-only column such as the ordinal.
        /// </summary>
        /// <param name="command">The command to populate.</param>
        /// <param name="tableNameHelper">Supplies the queue (body) table name.</param>
        /// <param name="rows">The serialized body and header bytes per message, in input order.</param>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table name is from configuration; values are parameterized")]
        internal static void BuildBodyMergeCommand(SqlCommand command,
            ITableNameHelper tableNameHelper,
            IReadOnlyList<(byte[] Body, byte[] Headers)> rows)
        {
            var builder = new StringBuilder();
            builder.Append("MERGE INTO ").Append(tableNameHelper.QueueName).AppendLine(" AS Target");
            builder.Append("USING (VALUES ");
            for (var i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append("(@Body").Append(i).Append(",@Headers").Append(i).Append(',').Append(i).Append(')');
            }
            builder.AppendLine(") AS Source (Body, Headers, Ordinal)");
            builder.AppendLine("ON 1 = 0");
            builder.AppendLine("WHEN NOT MATCHED THEN");
            builder.AppendLine("INSERT (Body, Headers) VALUES (Source.Body, Source.Headers)");
            builder.AppendLine("OUTPUT INSERTED.QueueID, Source.Ordinal;");

            command.CommandText = builder.ToString();

            for (var i = 0; i < rows.Count; i++)
            {
                command.Parameters.Add("@Body" + i, SqlDbType.VarBinary, -1).Value = rows[i].Body;
                command.Parameters.Add("@Headers" + i, SqlDbType.VarBinary, -1).Value = rows[i].Headers;
            }
        }
    }
}
