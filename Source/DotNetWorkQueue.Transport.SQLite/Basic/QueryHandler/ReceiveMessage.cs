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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    internal static class ReceiveMessage
    {
        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="metaTableName">Name of the meta table.</param>
        /// <param name="queueTableName">Name of the queue table.</param>
        /// <param name="forRpc">if set to <c>true</c> [for RPC].</param>
        /// <param name="statusTableName">Name of the status table.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public static CommandString GetDeQueueCommand(string metaTableName, 
            string queueTableName, 
            bool forRpc, 
            string statusTableName,
            SqLiteMessageQueueTransportOptions options)
        {
            var sb = new StringBuilder();

            var tempName = GenerateTempTableName();

            sb.AppendLine($"CREATE TEMP TABLE {tempName}(QueueID Integer PRIMARY KEY, CurrentDateTime Integer);");
            sb.AppendLine($"Insert into {tempName} (QueueID, CurrentDateTime)");
            sb.AppendLine("select  ");
            sb.AppendLine(metaTableName + ".QueueID, ");
            sb.AppendLine("@CurrentDateTime");
            sb.AppendLine($"from {metaTableName}  ");

            //calculate where clause...
            if (options.EnableStatus && options.EnableDelayedProcessing)
            {
                sb.Append($" WHERE {metaTableName}.Status = {Convert.ToInt16(QueueStatuses.Waiting)} ");
                sb.AppendLine("and QueueProcessTime < @CurrentDateTime ");
            }
            else if (options.EnableStatus)
            {
                sb.Append($" WHERE {metaTableName}.Status = {Convert.ToInt16(QueueStatuses.Waiting)} ");
            }
            else if (options.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (QueueProcessTime < @CurrentDateTime) ");
            }

            if (forRpc)
            {
                sb.AppendLine("AND SourceQueueID = @QueueID");
            }


            if (options.EnableMessageExpiration || options.QueueType == QueueTypes.RpcReceive || options.QueueType == QueueTypes.RpcSend)
            {
                sb.AppendLine("AND ExpirationTime > @CurrentDateTime");
            }

            //determine order by looking at the options
            var bNeedComma = false;
            sb.Append(" Order by  ");
            if (options.EnableStatus)
            {
                sb.Append(" status asc ");
                bNeedComma = true;
            }
            if (options.EnablePriority)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" priority asc ");
                bNeedComma = true;
            }
            if (options.EnableDelayedProcessing)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" QueueProcessTime asc ");
                bNeedComma = true;
            }
            if (options.EnableMessageExpiration)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" ExpirationTime asc ");
                bNeedComma = true;
            }

            if (bNeedComma)
            {
                sb.Append(", ");
            }
            sb.AppendLine($" {metaTableName}.QueueID asc  ");
            sb.AppendLine(" LIMIT 1;");


            //----------------------------
            sb.AppendLine("");
            sb.AppendLine("select  ");
            sb.AppendLine($"{tempName}.QueueID, {metaTableName}.CorrelationID, {queueTableName}.Body, {queueTableName}.Headers ");

            if (options.EnableStatus)
            {
                sb.Append($", {metaTableName}.status ");
            }
            if (options.EnableHeartBeat)
            {
                sb.Append($", {metaTableName}.HeartBeat ");
            }

            sb.AppendLine($"from {tempName}  ");
            sb.AppendLine($"JOIN {metaTableName}  ");
            sb.AppendLine($"ON {metaTableName}.QueueID = {tempName}.QueueID  ");
            sb.AppendLine($"JOIN {queueTableName}  ");
            sb.AppendLine($"ON {metaTableName}.QueueID = {queueTableName}.QueueID;  ");

            sb.AppendLine("");

            List<string> additionalCommands = new List<string>();

            //determine if performing update or delete...
            var status = new StringBuilder();
            if (options.EnableStatus)
            { //update

                status.Append($"update {metaTableName} set status = {Convert.ToInt16(QueueStatuses.Processing)} ");
                if (options.EnableHeartBeat)
                {
                    status.AppendLine($", HeartBeat = (select {tempName}.CurrentDateTime from {tempName} LIMIT 1) ");
                }
                status.Append($" where {metaTableName}.QueueID = (select {tempName}.QueueID from {tempName} LIMIT 1);");
            }
            else
            { //delete - note even if heartbeat is enabled, there is no point in setting it

                //a delete here if not using transactions will actually remove the record from the queue
                //it's up to the caller to handle error conditions in this case.
                status.AppendLine($"delete from {metaTableName} where {metaTableName}.QueueID = (select {tempName}.QueueID from {tempName} LIMIT 1); ");
            }

            additionalCommands.Add(status.ToString());

            if (options.EnableStatusTable)
            {
                additionalCommands.Add($" update {statusTableName} set status = {Convert.ToInt16(QueueStatuses.Processing)} where {statusTableName}.QueueID = (select {tempName}.QueueID from {tempName} LIMIT 1);");
            }

            //will drop when the connection closes
            //additionalCommands.Add($"drop table {tempName};");

            return new CommandString(sb.ToString(), additionalCommands);
        }

        private static string GenerateTempTableName()
        {
            var encoded = new UTF8Encoding().GetBytes(Guid.NewGuid().ToString());
            var hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encoded);
            return "I" + BitConverter.ToString(hash)
               .Replace("-", string.Empty)
               .Replace("_", string.Empty)
               .ToLower();
        }
    }
}
