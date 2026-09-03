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
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.QueryHandler
{
    internal static class ReceiveMessage
    {
        private const string DequeueKey = "dequeueCommand";

        /// <summary>Gets the de queue command.</summary>
        /// <param name="commandCache">The command cache.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="options">The options.</param>
        /// <param name="configuration">Queue Configuration</param>
        /// <param name="routes">The routes.</param>
        /// <param name="userParams">An optional collection of user params to pass to the query</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public static string GetDeQueueCommand(PostgreSqlCommandStringCache commandCache, ITableNameHelper tableNameHelper, PostgreSqlMessageQueueTransportOptions options, QueueConsumerConfiguration configuration, List<string> routes, out List<Npgsql.NpgsqlParameter> userParams)
        {
            userParams = null;
            var userQuery = configuration.GetUserClause();
            var cacheKey = BuildCacheKey(routes, userQuery);

            if (cacheKey != null && commandCache.Contains(cacheKey))
            {
                return commandCache.Get(cacheKey).CommandText;
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
                needWhere = false;

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

            //if true, the query can be added to via user settings
            if (options.AdditionalColumnsOnMetaData && !string.IsNullOrEmpty(userQuery))
            {
                userParams = configuration.GetUserParameters(); //NOTE - could be null
                sb.AppendLine(needWhere
                    ? $"where {userQuery} "
                    : $"AND {userQuery} ");
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

            //a null key means this shape is not cacheable - see BuildCacheKey
            return cacheKey == null ? sb.ToString() : commandCache.Add(cacheKey, sb.ToString());
        }

        /// <summary>
        /// The key the generated statement is cached under.
        /// </summary>
        /// <remarks>
        /// Routes used to bypass the cache, so a routed consumer rebuilt the whole statement on
        /// every poll - measured at 2,648 bytes a time on a loop that never stops. Only the route
        /// <em>count</em> reaches the text, since routes become <c>@Route1..@RouteN</c>
        /// placeholders and their values are bound as parameters, so the count is what it is keyed
        /// on. Counts are small integers, which keeps the cache to a handful of entries.
        /// <para>
        /// A user clause is deliberately <b>not</b> cached, and must not be.
        /// <c>SetUserParametersAndClause</c> takes a <c>Func&lt;string&gt;</c> that
        /// <c>GetUserClause</c> invokes on every de-queue, so the clause is free to differ each
        /// time. Keying on its text would put a new permanent entry in a cache that lives as long
        /// as the consumer, once per poll, until the process ran out of memory. An earlier version
        /// of this method did exactly that.
        /// </para>
        /// <para>
        /// Returning null means "do not cache this one", which leaves a user-clause consumer with
        /// the behaviour it had before any of this: the statement is rebuilt per poll.
        /// </para>
        /// </remarks>
        private static string BuildCacheKey(List<string> routes, string userQuery)
        {
            if (!string.IsNullOrEmpty(userQuery))
            {
                return null;
            }

            var routeCount = routes?.Count ?? 0;
            return routeCount == 0 ? DequeueKey : $"{DequeueKey}|routes={routeCount}";
        }
    }
}
