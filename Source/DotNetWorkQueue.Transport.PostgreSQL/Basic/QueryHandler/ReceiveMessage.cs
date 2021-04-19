// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    internal static class ReceiveMessage
    {
        private const string DequeueKey = "dequeueCommand";

        /// <summary>
        /// Gets the de queue command.
        /// </summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="options">The options.</param>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public static string GetDeQueueCommand(PostgreSqlCommandStringCache commandCache, ITableNameHelper tableNameHelper, PostgreSqlMessageQueueTransportOptions options, List<string> routes )
        {
            if (routes == null || routes.Count == 0)
            {
                if (commandCache.Contains(DequeueKey))
                {
                    return commandCache.Get(DequeueKey).CommandText;
                }
            }

            var sb = new StringBuilder();
            var needWhere = true;
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
                needWhere = false;
            }
            else if (options.EnableStatus)
            {
                sb.AppendFormat("WHERE q.Status = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                needWhere = false;
            }
            else if (options.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (q.QueueProcessTime < @CurrentDate) ");
                needWhere = false;
            }

            if (options.EnableMessageExpiration)
            {
                if (needWhere)
                {
                    sb.AppendLine("Where q.ExpirationTime > @CurrentDate ");
                    needWhere = false;
                }
                else
                {
                    sb.AppendLine("AND q.ExpirationTime > @CurrentDate ");
                }
            }

            if (options.EnableRoute && routes != null && routes.Count > 0)
            {
                sb.AppendLine(needWhere ? "where Route IN ( " : "AND Route IN ( ");

                for (var i = 1; i - 1 < routes.Count; i++)
                {
                    sb.Append("@Route" + i);
                    if (i != routes.Count)
                    {
                        sb.Append(", ");
                    }
                }
                sb.Append(") ");
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
            sb.AppendLine(" AND q.QueueID = qm.QueueID");
            sb.AppendLine("returning q.queueid, qm.body, qm.Headers, q.CorrelationID");

            if (routes != null && routes.Count > 0)
            { //TODO - cache based on route
                return sb.ToString();
            }
            return commandCache.Add(DequeueKey, sb.ToString());
        }
    }
}
