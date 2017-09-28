// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic.CommandHandler
{
    internal static class SendMessage
    {
        internal static IDbCommand CreateMetaDataRecord(TimeSpan? delay, TimeSpan expiration, IDbConnection connection,
            IMessage message, IAdditionalMessageData data, TableNameHelper tableNameHelper, 
            IHeaders headers, SqLiteMessageQueueTransportOptions options, IGetTime getTime)
        {
            var command = connection.CreateCommand();
            BuildMetaCommand(command, tableNameHelper, headers,
                data, message, 0, options, delay, expiration, getTime.GetCurrentUtcDate());
            return command;
        }


        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal static IDbCommand GetMainCommand(SendMessageCommand commandSend, 
            IDbConnection connection,
            IDbCommandStringCache commandCache,
            IHeaders headers,
            ICompositeSerialization serializer)
        {
            var command = connection.CreateCommand();
            command.CommandText = commandCache.GetCommand(CommandStringTypes.InsertMessageBody);
            var serialization =
                serializer.Serializer.MessageToBytes(new MessageBody { Body = commandSend.MessageToSend.Body });

            var param = command.CreateParameter();
            param.ParameterName = "@Body";
            param.DbType = DbType.Binary;
            param.Value = serialization.Output;
            command.Parameters.Add(param);

            commandSend.MessageToSend.SetHeader(headers.StandardHeaders.MessageInterceptorGraph,
                serialization.Graph);

            param = command.CreateParameter();
            param.ParameterName = "@Headers";
            param.DbType = DbType.Binary;
            param.Value = serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);
            command.Parameters.Add(param);

            return command;
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        internal static void BuildStatusCommand(IDbCommand command,
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
            builder.Append($"@QueueID, {Convert.ToInt32(QueueStatuses.Waiting)}, @CorrelationID");

            //add configurable column value - user
            AddUserColumnsValues(builder, data);

            builder.Append(")"); //close the VALUES 

            command.CommandText = builder.ToString();

            options.AddBuiltInColumnsParams(command, data, null, TimeSpan.Zero, currentDateTime);

            if (id > 0)
            {
                var paramid = command.CreateParameter();
                paramid.ParameterName = "@QueueID";
                paramid.DbType = DbType.Int64;
                paramid.Value = id;
                command.Parameters.Add(paramid);
            }

            var param = command.CreateParameter();
            param.ParameterName = "@CorrelationID";
            param.DbType = DbType.StringFixedLength;
            param.Size = 38;
            param.Value = data.CorrelationId.Id.Value.ToString();
            command.Parameters.Add(param);

            //add configurable column command params - user
            AddUserColumnsParams(command, data);
            AddHeaderColumnParams(command, message, headers);
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query checked")]
        private static void BuildMetaCommand(IDbCommand command, 
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
            options.AddBuiltInColumnValues(sbMeta);

            AddHeaderValues(sbMeta, message, headers);

            sbMeta.Append(")"); //close the VALUES 

            command.CommandText = sbMeta.ToString();

            options.AddBuiltInColumnsParams(command, data, delay, expiration, currentDateTime);
            AddHeaderColumnParams(command, message, headers);

            if (id > 0)
            {
                var paramid = command.CreateParameter();
                paramid.ParameterName = "@QueueID";
                paramid.DbType = DbType.Int64;
                paramid.Value = id;
                command.Parameters.Add(paramid);
            }

            var param = command.CreateParameter();
            param.ParameterName = "@CorrelationID";
            param.DbType = DbType.StringFixedLength;
            param.Size = 38;
            param.Value = data.CorrelationId.Id.Value.ToString();
            command.Parameters.Add(param);

            param = command.CreateParameter();
            param.ParameterName = "@CurrentDate";
            param.DbType = DbType.Int64;
            param.Value = currentDateTime.Ticks;
            command.Parameters.Add(param);
        }
        /// <summary>
        /// Adds the SQL command params for the user specific meta data
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        private static void AddUserColumnsParams(IDbCommand command, IAdditionalMessageData data)
        {
            foreach (var metadata in data.AdditionalMetaData)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@" + metadata.Name;
                param.Value = metadata.Value;
                command.Parameters.Add(param);
            }
        }

        /// <summary>
        /// Adds the header column parameters.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="data">The data.</param>
        /// <param name="headers">The headers.</param>
        private static void AddHeaderColumnParams(IDbCommand command, IMessage data, IHeaders headers)
        {
            var responseId = data.GetInternalHeader(headers.StandardHeaders.RpcResponseId);
            if (string.IsNullOrEmpty(responseId)) return;
            var param = command.CreateParameter();
            param.ParameterName = "@SourceQueueID";
            param.DbType = DbType.Int64;
            param.Value = long.Parse(responseId);
            command.Parameters.Add(param);
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
                    command.Append(",");
                }
                command.Append(metadata.Name);
                if (i < data.AdditionalMetaData.Count - 1)
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
            foreach (var metadata in data.AdditionalMetaData)
            {
                if (i == 0)
                {
                    command.Append(",");
                }
                command.Append("@" + metadata.Name);
                if (i < data.AdditionalMetaData.Count - 1)
                {
                    command.Append(",");
                }
                i++;
            }
        }
    }
}
