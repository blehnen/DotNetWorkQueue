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

            if (commandCache.Contains(cacheKey))
            {
                //The parameters are not part of the text, so they are produced whether the text
                //was cached or built. Returning the cached statement without this would leave a
                //consumer with a user clause holding a statement whose parameters were never
                //supplied.
                if (options.AdditionalColumnsOnMetaData && !string.IsNullOrEmpty(userQuery))
                {
                    userParams = configuration.GetUserParameters(); //NOTE - could be null
                }
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

            return commandCache.Add(cacheKey, sb.ToString());
        }

        /// <summary>
        /// The key the generated statement is cached under.
        /// </summary>
        /// <remarks>
        /// Routes and a user clause used to bypass the cache, so a consumer using either rebuilt
        /// the whole statement on every poll - measured at 2,648 bytes a time on a loop that never
        /// stops.
        /// <para>
        /// Both are safe to key on. Routes become <c>@Route1..@RouteN</c> placeholders, so only
        /// their <em>count</em> reaches the text and never their values; the user clause is
        /// inlined, so the clause itself is part of the key. Both are fixed for the life of a
        /// consumer, which keeps this at one entry per consumer shape rather than one per poll.
        /// </para>
        /// <para>
        /// The plain key is kept for the common case so that statement, and its behaviour, are
        /// exactly what they were before.
        /// </para>
        /// </remarks>
        private static string BuildCacheKey(List<string> routes, string userQuery)
        {
            var routeCount = routes?.Count ?? 0;
            if (routeCount == 0 && string.IsNullOrEmpty(userQuery))
            {
                return DequeueKey;
            }

            return $"{DequeueKey}|routes={routeCount}|user={userQuery}";
        }
    }
}
