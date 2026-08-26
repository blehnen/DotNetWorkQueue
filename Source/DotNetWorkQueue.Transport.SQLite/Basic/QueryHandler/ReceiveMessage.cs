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
using System.Data.SQLite;
using System.Text;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Basic.QueryHandler
{
    internal static class ReceiveMessage
    {
        /// <summary>Gets the de queue command.</summary>
        /// <param name="metaTableName">Name of the meta table.</param>
        /// <param name="queueTableName">Name of the queue table.</param>
        /// <param name="statusTableName">Name of the status table.</param>
        /// <param name="options">The options.</param>
        /// <param name="routes">The routes.</param>
        /// <param name="userClause">The caller's additional where clause, if any</param>
        /// <returns>
        ///   <br />
        /// </returns>
        public static CommandString GetDeQueueCommand(string metaTableName,
            string queueTableName,
            string statusTableName,
            SqLiteMessageQueueTransportOptions options,
            string userClause,
            List<string> routes)
        {
            var sb = new StringBuilder();

            var tempName = TempTableName(metaTableName);

            //Created once per connection rather than once per dequeue. A temp table lives until its
            //connection closes, and since connections are now pooled and held open for the life of
            //the queue, a uniquely named table per dequeue accumulated one table per message
            //consumed - measured at 46.7 us to create at 100 messages and 346.1 us at 5,000, still
            //climbing, with every one of them resident. The name is derived from the queue instead,
            //so the statement is a no-op after the first dequeue on a given connection.
            sb.AppendLine($"CREATE TEMP TABLE IF NOT EXISTS {tempName}(QueueID Integer PRIMARY KEY, CurrentDateTime Integer);");

            //a dequeue that committed leaves its row behind for the next one to clear
            sb.AppendLine($"DELETE FROM {tempName};");
            sb.AppendLine($"Insert into {tempName} (QueueID, CurrentDateTime)");
            sb.AppendLine("select  ");
            sb.AppendLine(metaTableName + ".QueueID, ");
            sb.AppendLine("@CurrentDateTime");
            sb.AppendLine($"from {metaTableName}  ");

            //calculate where clause...
            var needWhere = true;
            if (options.EnableStatus && options.EnableDelayedProcessing)
            {
                sb.Append($" WHERE {metaTableName}.Status = {Convert.ToInt16(QueueStatuses.Waiting)} ");
                sb.AppendLine("and QueueProcessTime < @CurrentDateTime ");
                needWhere = false;
            }
            else if (options.EnableStatus)
            {
                sb.Append($" WHERE {metaTableName}.Status = {Convert.ToInt16(QueueStatuses.Waiting)} ");
                needWhere = false;
            }
            else if (options.EnableDelayedProcessing)
            {
                sb.AppendLine("WHERE (QueueProcessTime < @CurrentDateTime) ");
                needWhere = false;
            }

            if (options.EnableMessageExpiration)
            {
                if (needWhere)
                {
                    sb.AppendLine("where ExpirationTime > @CurrentDateTime ");
                    needWhere = false;
                }
                else
                {
                    sb.AppendLine("AND ExpirationTime > @CurrentDateTime ");
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
            if (options.AdditionalColumnsOnMetaData && !string.IsNullOrEmpty(userClause))
            {
                sb.AppendLine(needWhere
                    ? $"where {userClause} "
                    : $"AND {userClause} ");
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

            var additionalCommands = new List<string>();

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

            //the temp table is reused rather than dropped; see TempTableName

            return new CommandString(sb.ToString(), additionalCommands);
        }

        /// <summary>
        /// The name of the temp table a dequeue stages its candidate row in.
        /// </summary>
        /// <remarks>
        /// Derived from the queue rather than generated per call, so that repeated dequeues on one
        /// connection reuse a single table. Temp tables are private to a connection and live in the
        /// temp schema, so this cannot collide with a caller's own tables; deriving it from the
        /// meta table name keeps two queues sharing a connection string apart.
        /// </remarks>
        private static string TempTableName(string metaTableName)
        {
            //The meta table name is already a valid identifier - it is used unquoted throughout
            //these statements - so it needs no hashing to become one. The previous name hashed a
            //GUID only because a GUID is not a legal identifier.
            return metaTableName + "TempDequeue";
        }
    }
}
