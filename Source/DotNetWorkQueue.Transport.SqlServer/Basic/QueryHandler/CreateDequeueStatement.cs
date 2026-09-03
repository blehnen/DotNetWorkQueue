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
using Microsoft.Data.SqlClient;
using System.Net;
using System.Text;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler
{
    internal class CreateDequeueStatement
    {
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly ITableNameHelper _tableNameHelper;
        private readonly SqlServerCommandStringCache _commandCache;
        private readonly QueueConsumerConfiguration _configuration;

        private const string DequeueKey = "dequeueCommand";

        public CreateDequeueStatement(ISqlServerMessageQueueTransportOptionsFactory optionsFactory,
            ITableNameHelper tableNameHelper,
            SqlServerCommandStringCache commandCache,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(optionsFactory);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(commandCache);
            Guard.NotNull(configuration);

            _options = new Lazy<SqlServerMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _commandCache = commandCache;
            _configuration = configuration;
        }

        /// <summary>Gets the de queue command.</summary>
        /// <param name="userParams">The optional user de-queue params</param>
        /// <param name="routes">The routes.</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public string GetDeQueueCommand(out List<SqlParameter> userParams, List<string> routes = null)
        {
            userParams = null;
            var userQuery = _configuration.GetUserClause();
            var cacheKey = BuildCacheKey(routes, userQuery);

            if (cacheKey != null && _commandCache.Contains(cacheKey))
            {
                return _commandCache.Get(cacheKey).CommandText;
            }

            var sb = new StringBuilder();

            //NOTE - this could be optimized a little bit. We are always using a CTE, but that's not necessary if the queue is 
            //setup as a pure FIFO queue.

            sb.AppendLine("declare @Queue1 table ");
            sb.AppendLine("( ");
            sb.AppendLine("QueueID bigint, ");
            sb.AppendLine("CorrelationID uniqueidentifier ");
            sb.AppendLine("); ");
            sb.AppendLine("with cte as ( ");
            sb.AppendLine("select top(1)  ");
            sb.AppendLine(_tableNameHelper.MetaDataName + ".QueueID, CorrelationID ");

            if (_options.Value.EnableStatus)
            {
                sb.Append(", [status] ");
            }
            if (_options.Value.EnableHeartBeat)
            {
                sb.Append(", HeartBeat ");
            }

            sb.AppendLine($"from {_tableNameHelper.MetaDataName} with (updlock, readpast, rowlock) ");

            //calculate where clause...
            var needWhere = true;
            if (_options.Value.EnableStatus && _options.Value.EnableDelayedProcessing)
            {
                sb.AppendFormat(" WHERE [Status] = {0} ", Convert.ToInt16(QueueStatuses.Waiting));
                sb.AppendLine("and QueueProcessTime < getutcdate() ");
                needWhere = false;
            }
            else if (_options.Value.EnableStatus)
            {
                sb.AppendFormat("WHERE [Status] = {0}  ", Convert.ToInt16(QueueStatuses.Waiting));
                needWhere = false;
            }
            else if (_options.Value.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (QueueProcessTime < getutcdate()) ");
                needWhere = false;
            }

            if (_options.Value.EnableRoute && routes != null && routes.Count > 0)
            {
                if (needWhere)
                {
                    sb.AppendLine("where Route IN ( ");
                    needWhere = false;
                }
                else
                {
                    sb.AppendLine("AND Route IN ( ");
                }

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

            if (_options.Value.EnableMessageExpiration)
            {
                sb.AppendLine(needWhere
                    ? "where ExpirationTime > getutcdate() "
                    : "AND ExpirationTime > getutcdate() ");
                needWhere = false;
            }

            //if true, the query can be added to via user settings
            if (_options.Value.AdditionalColumnsOnMetaData && !string.IsNullOrEmpty(userQuery))
            {
                userParams = _configuration.GetUserParameters(); //NOTE - could be null
                sb.AppendLine(needWhere
                    ? $"where {userQuery} "
                    : $"AND {userQuery} ");
            }

            //determine order by looking at the options
            var bNeedComma = false;
            sb.Append(" Order by ");
            if (_options.Value.EnableStatus)
            {
                sb.Append(" [status] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnablePriority)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.Append(" [priority] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [QueueProcessTime] asc ");
                bNeedComma = true;
            }
            if (_options.Value.EnableMessageExpiration)
            {
                if (bNeedComma)
                {
                    sb.Append(", ");
                }
                sb.AppendLine(" [ExpirationTime] asc ");
                bNeedComma = true;
            }

            if (bNeedComma)
            {
                sb.Append(", ");
            }
            sb.AppendLine(" [QueueID] asc ) ");

            //determine if performing update or delete...
            if (_options.Value.EnableStatus && !_options.Value.EnableHoldTransactionUntilMessageCommitted)
            { //update

                sb.AppendFormat("update cte set status = {0} ", Convert.ToInt16(QueueStatuses.Processing));
                if (_options.Value.EnableHeartBeat)
                {
                    sb.AppendLine(", HeartBeat = GetUTCDate() ");
                }
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else if (_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                sb.AppendLine("update cte set queueid = QueueID ");
                sb.AppendLine("output inserted.QueueID, inserted.CorrelationID into @Queue1 ");
            }
            else
            { //delete - note even if heartbeat is enabled, there is no point in setting it

                //a delete here if not using transactions will actually remove the record from the queue
                //it's up to the caller to handle error conditions in this case.
                sb.AppendLine("delete from cte ");
                sb.AppendLine("output deleted.QueueID, deleted.CorrelationID into @Queue1 ");
            }

            //grab the rest of the data - this is all standard
            sb.AppendLine("select q.queueid, qm.body, qm.Headers, q.CorrelationID from @Queue1 q ");
            sb.AppendLine($"INNER JOIN {_tableNameHelper.QueueName} qm with (nolock) "); //a dirty read on the data here should be ok, since we have exclusive access to the queue record on the meta data table
            sb.AppendLine("ON q.QueueID = qm.QueueID  ");

            //if we are holding transactions, we can't update the status table as part of this query - has to be done after de-queue instead
            if (_options.Value.EnableStatusTable && !_options.Value.EnableHoldTransactionUntilMessageCommitted)
            {
                sb.AppendFormat("update {0} set status = {1} where {0}.QueueID = (select q.queueid from @Queue1 q)", _tableNameHelper.StatusName, Convert.ToInt16(QueueStatuses.Processing));
            }

            //a null key means this shape is not cacheable - see BuildCacheKey
            return cacheKey == null ? sb.ToString() : _commandCache.Add(cacheKey, sb.ToString());
        }

        /// <summary>
        /// The key the generated statement is cached under, or null when this shape must not be
        /// cached.
        /// </summary>
        /// <remarks>
        /// Routes used to bypass the cache, so a routed consumer rebuilt the whole statement - a
        /// table variable, a CTE and forty-odd appends - on every poll: 5,368 bytes a time, 45% of
        /// everything an empty de-queue allocated, on a loop that never stops. Only the route
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
        private string BuildCacheKey(List<string> routes, string userQuery)
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
