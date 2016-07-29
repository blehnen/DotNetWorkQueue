// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace DotNetWorkQueue.Transport.SQLite.Basic.CommandHandler
{
    internal static class SendMessage
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal static void BuildStatusCommand(SQLiteCommand command,
            TableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            long id,
            SqLiteMessageQueueTransportOptions options,
            DateTime currentDateTime)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Insert into " + tableNameHelper.StatusName);
            builder.Append("(QueueID, Status, CorrelationID ");

            //add configurable columns - user
            AddUserColumns(builder, data);

            //close the column list
            builder.AppendLine(") ");

            //add standard values that are always present
            builder.Append("VALUES (");
            builder.Append($"@QueueID, {Convert.ToInt32(QueueStatus.Waiting)}, @CorrelationID");

            //add configurable column value - user
            AddUserColumnsValues(builder, data);

            builder.Append(")"); //close the VALUES 

            command.CommandText = builder.ToString();

            options.AddBuiltInColumnsParams(command, data, null, TimeSpan.Zero, currentDateTime);

            command.Parameters.Add("@QueueID", DbType.Int64, 8).Value = id;
            command.Parameters.Add("@CorrelationID", DbType.StringFixedLength, 38).Value = data.CorrelationId.Id.Value.ToString();

            //add configurable column command params - user
            AddUserColumnsParams(command, data);
            AddHeaderColumnParams(command, message, headers);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal static void BuildMetaCommand(SQLiteCommand command, 
            TableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            long id,
            SqLiteMessageQueueTransportOptions options,
            TimeSpan? delay, 
            TimeSpan expiration,
            DateTime currentDateTime)
        {
            var sbMeta = new StringBuilder();
            sbMeta.AppendLine("Insert into " + tableNameHelper.MetaDataName);
            sbMeta.Append("(QueueID, CorrelationID, QueuedDateTime ");

            //add configurable columns - queue
            options.AddBuiltInColumns(sbMeta);

            AddHeaderColumns(sbMeta, message, headers);

            //close the column list
            sbMeta.AppendLine(") ");

            //add standard values that are always present
            sbMeta.Append("VALUES (");
            sbMeta.Append("@QueueID, @CorrelationID, @CurrentDate ");

            //add the values for built in fields
            options.AddBuiltInColumnValues(delay, expiration, sbMeta);

            AddHeaderValues(sbMeta, message, headers);

            sbMeta.Append(")"); //close the VALUES 

            command.CommandText = sbMeta.ToString();

            options.AddBuiltInColumnsParams(command, data, delay, expiration, currentDateTime);
            AddHeaderColumnParams(command, message, headers);

            command.Parameters.Add("@QueueID", DbType.Int64, 8).Value = id;
            command.Parameters.Add("@CorrelationID", DbType.StringFixedLength, 38).Value = data.CorrelationId.Id.Value.ToString();
            command.Parameters.Add("@CurrentDate", DbType.Int64).Value = currentDateTime.Ticks;

        }
        /// <summary>
        /// Adds the SQL command params for the user specific meta data
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsParams(SQLiteCommand command, IAdditionalMessageData data)
        {
            var list = data.AdditionalMetaData.Where(x => !x.Name.StartsWith("@"));
            foreach (var metadata in list)
            {
                command.Parameters.AddWithValue("@" + metadata.Name, metadata.Value);
            }
        }

        /// <summary>
        /// Adds the header column parameters.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <param name="headers">The headers.</param>
        private static void AddHeaderColumnParams(SQLiteCommand command, IMessage data, IHeaders headers)
        {
            var responseId = data.GetInternalHeader(headers.StandardHeaders.RpcResponseId);
            if (!string.IsNullOrEmpty(responseId))
            {
                command.Parameters.AddWithValue("@SourceQueueID", long.Parse(responseId));
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
            var list = data.AdditionalMetaData.Where(x => !x.Name.StartsWith("@")).ToList();
            foreach (var metadata in list)
            {
                if (i == 0)
                {
                    command.Append(",");
                }
                command.Append(metadata.Name);
                if (i < list.Count - 1)
                {
                    command.Append(",");
                }
                i++;
            }
        }
        /// <summary>
        /// Adds the header columns.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <param name="headers">The headers.</param>
        private static void AddHeaderColumns(StringBuilder command, IMessage data, IHeaders headers)
        {
            var responseId = data.GetInternalHeader(headers.StandardHeaders.RpcResponseId);
            if (string.IsNullOrEmpty(responseId)) return;
            command.Append(",SourceQueueID");
        }

        /// <summary>
        /// Adds the header values.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <param name="headers">The headers.</param>
        private static void AddHeaderValues(StringBuilder command, IMessage data, IHeaders headers)
        {
            var responseId = data.GetInternalHeader(headers.StandardHeaders.RpcResponseId);
            if (string.IsNullOrEmpty(responseId)) return;
            command.Append(",@SourceQueueID");
        }
        /// <summary>
        /// Adds the values for the user specific meta data to the SQL command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsValues(StringBuilder command, IAdditionalMessageData data)
        {
            var i = 0;
            var list = data.AdditionalMetaData.Where(x => !x.Name.StartsWith("@")).ToList();
            foreach (var metadata in list)
            {
                if (i == 0)
                {
                    command.Append(",");
                }
                command.Append("@" + metadata.Name);
                if (i < list.Count - 1)
                {
                    command.Append(",");
                }
                i++;
            }
        }
    }
}
