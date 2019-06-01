using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler
{
    internal static class SendMessage
    {
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildStatusCommand(SqlCommand command,
            TableNameHelper tableNameHelper,
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

            options.AddBuiltInColumnsParams(command, data);

            command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
            command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;

            //add configurable column command params - user
            AddUserColumnsParams(command, data);
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Query OK")]
        internal static void BuildMetaCommand(SqlCommand command, 
            TableNameHelper tableNameHelper,
            IHeaders headers,
            IAdditionalMessageData data,
            IMessage message,
            long id,
            SqlServerMessageQueueTransportOptions options,
            TimeSpan? delay, 
            TimeSpan expiration)
        {
            var sbMeta = new StringBuilder();
            sbMeta.AppendLine("Insert into " + tableNameHelper.MetaDataName);
            sbMeta.Append("(QueueID, CorrelationID, QueuedDateTime ");

            //add configurable columns - queue
            options.AddBuiltInColumns(sbMeta);

            //close the column list
            sbMeta.AppendLine(") ");

            //add standard values that are always present
            sbMeta.Append("VALUES (");
            sbMeta.Append("@QueueID, @CorrelationID, GetUTCDate() ");

            //add the values for built in fields
            options.AddBuiltInColumnValues(delay, expiration, sbMeta);

            sbMeta.Append(")"); //close the VALUES 

            command.CommandText = sbMeta.ToString();

            options.AddBuiltInColumnsParams(command, data);
 
            command.Parameters.Add("@QueueID", SqlDbType.BigInt, 8).Value = id;
            command.Parameters.Add("@CorrelationID", SqlDbType.UniqueIdentifier, 16).Value = data.CorrelationId.Id.Value;

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
