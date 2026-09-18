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
using System.Data;
using System.Data.Common;
using System.Text;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Schema version 1: one error tracking row per message per exception type.
    /// </summary>
    /// <remarks>
    /// The unique index on (QueueID, ExceptionType) arrived in #299, but only for queues created
    /// after it. An older queue kept the check-then-write path in SetErrorCountCommandHandler, where
    /// two failures of the same message arriving together can each see no row and each insert one.
    /// The retry count then reads low, so the message gets more attempts than configured and a poison
    /// message can loop instead of reaching the error queue. Until now the only way out was to
    /// re-create the queue; this version is that fix applied in place.
    ///
    /// Duplicates already written have to go before the index can exist, and the count they collapse
    /// to is the largest of them rather than their sum: SetErrorCountCommand documents retryCount as
    /// "the total number of failures of this exception type, including the one being recorded", so
    /// two rows holding 3 and 4 are two answers to one question rather than two halves of it. Adding
    /// them would invent failures that never happened and send the message to the error queue early.
    /// </remarks>
    public abstract class AErrorTrackingUniqueIndexVersion : ISchemaVersion
    {
        private readonly CommandStringCache _commandCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="AErrorTrackingUniqueIndexVersion"/> class.
        /// </summary>
        /// <param name="commandCache">The command cache, which holds the statement asking whether the index is already there.</param>
        protected AErrorTrackingUniqueIndexVersion(CommandStringCache commandCache)
        {
            Guard.NotNull(commandCache);
            _commandCache = commandCache;
        }

        /// <inheritdoc />
        public string Script(ITableNameHelper tableNames, DbConnection connection, DbTransaction transaction)
        {
            Guard.NotNull(tableNames);
            Guard.NotNull(connection);
            Guard.NotNull(transaction);

            var table = tableNames.ErrorTrackingName;
            var script = new StringBuilder();

            script.AppendLine(CollapseDuplicateRowsScript(table));

            //A queue created after #299 already has the index while still reading as version zero,
            //because the version table did not exist yet. Creating it again would fail and take the
            //whole upgrade with it, so ask first - by shape, which also covers an index someone added
            //by hand under a name of their own choosing.
            if (!UniqueIndexExists(table, connection, transaction))
            {
                script.AppendLine(CreateUniqueIndexScript(table));
            }

            return script.ToString();
        }

        /// <summary>
        /// The statement creating the unique index on (QueueID, ExceptionType).
        /// </summary>
        /// <param name="errorTrackingTable">The error tracking table, named as this transport addresses it.</param>
        /// <remarks>
        /// Written out per transport rather than taken from the schema classes that create a new queue.
        /// A shipped version is immutable: if the index the creation path builds ever changes, this
        /// version must still produce what it produced the day it shipped, or a queue that already
        /// applied it would believe something untrue about itself.
        /// </remarks>
        protected abstract string CreateUniqueIndexScript(string errorTrackingTable);

        /// <summary>
        /// The statements collapsing duplicate rows down to one per (QueueID, ExceptionType).
        /// </summary>
        /// <param name="errorTrackingTable">The error tracking table, named as this transport addresses it.</param>
        /// <remarks>
        /// Plain SQL that all three transports accept. The surviving row is the lowest ErrorTrackingID
        /// of the group and takes the largest retry count in it; the update is restricted to groups
        /// that actually have duplicates, so a queue with none pays for two scans and writes nothing.
        ///
        /// Order matters: the update has to run while every duplicate is still there, because the
        /// largest count may well be on a row the delete is about to remove.
        /// </remarks>
        protected virtual string CollapseDuplicateRowsScript(string errorTrackingTable)
        {
            return $@"UPDATE {errorTrackingTable}
                      SET RetryCount =
                      (
                          SELECT MAX(d.RetryCount) FROM {errorTrackingTable} d
                          WHERE d.QueueID = {errorTrackingTable}.QueueID
                            AND d.ExceptionType = {errorTrackingTable}.ExceptionType
                      )
                      WHERE ErrorTrackingID IN
                      (
                          SELECT MIN(k.ErrorTrackingID) FROM {errorTrackingTable} k
                          GROUP BY k.QueueID, k.ExceptionType
                          HAVING COUNT(*) > 1
                      );

                      DELETE FROM {errorTrackingTable}
                      WHERE ErrorTrackingID NOT IN
                      (
                          SELECT MIN(k.ErrorTrackingID) FROM {errorTrackingTable} k
                          GROUP BY k.QueueID, k.ExceptionType
                      );";
        }

        /// <summary>
        /// Whether the table already carries a unique index of the right shape.
        /// </summary>
        /// <remarks>
        /// Runs on the upgrade's own connection and transaction, so it sees the schema the rest of this
        /// upgrade is working against rather than a committed snapshot of it.
        /// </remarks>
        private bool UniqueIndexExists(string errorTrackingTable, DbConnection connection, DbTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = _commandCache.GetCommand(CommandStringTypes.GetErrorTrackingUniqueIndexExists);

                var table = command.CreateParameter();
                table.ParameterName = "@Table";
                table.DbType = DbType.AnsiString;
                table.Value = errorTrackingTable;
                command.Parameters.Add(table);

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }
    }
}
