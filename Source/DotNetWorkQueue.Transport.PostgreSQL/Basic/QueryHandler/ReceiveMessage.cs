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
using System.Text;
namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    internal static class ReceiveMessage
    {
        private const string RpcdequeueKey = "dequeueCommandRpc";
        private const string DequeueKey = "dequeueCommand";

        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="options">The options.</param>
        /// <param name="forRpc">if set to <c>true</c> [for RPC].</param>
        /// <returns></returns>
        public static string GetDeQueueCommand(PostgreSqlCommandStringCache commandCache, TableNameHelper tableNameHelper, PostgreSqlMessageQueueTransportOptions options, bool forRpc)
        {
            if (forRpc && commandCache.Contains(RpcdequeueKey))
            {
                return commandCache.Get(RpcdequeueKey);
            }
            if (commandCache.Contains(DequeueKey))
            {
                return commandCache.Get(DequeueKey);
            }

            var sb = new StringBuilder();
            if (options.EnableStatus)
            {
                sb.AppendLine($"update {tableNameHelper.MetaDataName} q");
                sb.AppendLine($"set status = {Convert.ToInt16(QueueStatuses.Processing)}");
                if (options.EnableHeartBeat)
                {
                    sb.AppendLine(", HeartBeat = @CurrentDate");
                }
                sb.AppendLine($"from {tableNameHelper.QueueName} qm");
            }
            else
            {
                sb.AppendLine($"delete from {tableNameHelper.MetaDataName} q ");
                sb.AppendLine($"using {tableNameHelper.QueueName} qm ");
            }

            sb.AppendLine(" where q.QueueID in (");
            sb.AppendLine($"select q.QueueID from {tableNameHelper.MetaDataName} q");

            //calculate where clause...
            if (options.EnableStatus && options.EnableDelayedProcessing)
            {
                sb.AppendFormat(" WHERE q.Status = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                sb.AppendLine("and q.QueueProcessTime < @CurrentDate ");
            }
            else if (options.EnableStatus)
            {
                sb.AppendFormat("WHERE q.Status = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
            }
            else if (options.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (q.QueueProcessTime < @CurrentDate) ");
            }

            if (forRpc)
            {
                sb.AppendLine("AND q.SourceQueueID = @QueueID");
            }


            if (options.EnableMessageExpiration || options.QueueType == QueueTypes.RpcReceive || options.QueueType == QueueTypes.RpcSend)
            {
                sb.AppendLine("AND q.ExpirationTime > @CurrentDate");
            }

            //determine order by looking at the options
            var bNeedComma = false;
            sb.Append(" Order by ");
            if (options.EnableStatus)
            {
                sb.Append(" q.status asc ");
                bNeedComma = true;
            }
            if (options.EnablePriority)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" q.priority asc ");
                bNeedComma = true;
            }
            if (options.EnableDelayedProcessing)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" q.QueueProcessTime asc ");
                bNeedComma = true;
            }
            if (options.EnableMessageExpiration)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" q.ExpirationTime asc ");
                bNeedComma = true;
            }

            if (bNeedComma)
            {
                sb.Append(", ");
            }
            sb.AppendLine(" q.QueueID asc limit 1 FOR UPDATE SKIP LOCKED) ");
            sb.AppendLine("returning q.queueid, qm.body, qm.Headers, q.CorrelationID");

            return commandCache.Add(forRpc ? RpcdequeueKey : DequeueKey, sb.ToString());
        }
    }
}
