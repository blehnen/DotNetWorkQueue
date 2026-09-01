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
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler
{
    /// <summary>
    /// Dequeues a message.
    /// </summary>
    internal class ReceiveMessageQueryHandler : IQueryHandler<ReceiveMessageQuery, IReceivedMessageInternal>
    {
        private static readonly object Reader = new object();

        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly TableNameHelper _tableNameHelper;
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly DatabaseExists _databaseExists;
        private readonly MessageDeQueue _messageDeQueue;

        /// <summary>
        /// Where the next poll resumes its search. Only ever read or written inside
        /// <see cref="Reader"/>, which every de-queue already holds.
        /// </summary>
        private int _resumeAfterId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQueryHandler"/> class.
        /// </summary>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="databaseExists">The database exists.</param>
        /// <param name="messageDeQueue">The message de queue.</param>
        public ReceiveMessageQueryHandler(ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            TableNameHelper tableNameHelper,
            LiteDbConnectionManager connectionInformation,
            DatabaseExists databaseExists,
            MessageDeQueue messageDeQueue)
        {
            Guard.NotNull(optionsFactory);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(databaseExists);
            Guard.NotNull(messageDeQueue);
            Guard.NotNull(connectionInformation);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _tableNameHelper = tableNameHelper;
            _connectionInformation = connectionInformation;
            _databaseExists = databaseExists;
            _messageDeQueue = messageDeQueue;
        }

        /// <summary>
        /// Handles the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public IReceivedMessageInternal Handle(ReceiveMessageQuery query)
        {
            if (!_databaseExists.Exists())
            {
                return null;
            }

            //ensure created
            if (!_options.IsValueCreated)
                _options.Value.ValidConfiguration();

            using (var db = _connectionInformation.GetDatabase())
            {
                lock (Reader) //we have to enforce a single de-queue action per process, as BeginTrans does not block in direct or memory mode
                {
                    db.Database.BeginTrans(); //will block in shared mode, but not direct or memory
                    try
                    {
                        var record = DequeueRecord(query, db.Database);
                        if (record != null)
                        {
                            return _messageDeQueue.HandleMessage(record.Item1, record.Item2.QueueId,
                                record.Item2.CorrelationId);
                        }
                    }
                    finally
                    {
                        db.Database.Commit();
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Walks the queue in insertion order and returns the first message that can be processed.
        /// </summary>
        /// <remarks>
        /// The eligibility tests used to be <c>Where</c> clauses. LiteDB chooses a single index per
        /// query, and it chose <c>Status</c> - where every waiting row holds the same value - so it
        /// selected the entire backlog and then sorted it. That made a de-queue cost grow with queue
        /// depth: measured at 2.3 ms against a thousand waiting messages and 31 ms against ten
        /// thousand, allocating 22 MB to find one message.
        /// <para>
        /// Leaving only the key in the <c>Where</c> forces the ordered walk instead, and the
        /// predicates run over a small window. Measured at 55 us regardless of depth. The key is
        /// <see cref="Schema.MetaDataTable.Id"/> rather than <c>QueuedDateTime</c> for two reasons:
        /// it is unique, so paging cannot step over messages that share a timestamp - a batch gives
        /// many messages the same one - and it is the primary key, so it is always indexed and this
        /// needs no schema change.
        /// </para>
        /// </remarks>
        private Schema.MetaDataTable FindNextEligible(ReceiveMessageQuery query,
            ILiteCollection<Schema.MetaDataTable> col)
        {
            var nowUtc = DateTime.UtcNow;
            var routes = _options.Value.EnableRoute && query.Routes != null && query.Routes.Count > 0
                ? query.Routes
                : null;

            var after = _resumeAfterId;
            for (var pages = 0; pages < MaxPagesPerPoll; pages++)
            {
                var page = col.Query()
                    .Where(x => x.Id > after)
                    .OrderBy(x => x.Id)
                    .Limit(PageSize)
                    .ToList();

                if (page.Count == 0)
                {
                    //end of the collection; the next poll starts from the head again
                    _resumeAfterId = 0;
                    return null;
                }

                foreach (var row in page)
                {
                    if (!IsEligible(row, nowUtc, routes)) continue;

                    //found one, so the next poll starts at the head: consecutive de-queues stay in
                    //order rather than continuing from wherever this search happened to end
                    _resumeAfterId = 0;
                    return row;
                }

                after = page[page.Count - 1].Id;
            }

            //Nothing eligible in the rows examined, and there are more to look at. Rather than read
            //the rest of the queue now, remember the position and carry on from here next time.
            //
            //Without this, a queue whose messages are all deferred - or a route that matches none of
            //them - would have every poll read the whole collection: measured at 12 ms against ten
            //thousand rows, which is worse than the scan this replaced. Resuming bounds a poll to
            //MaxPagesPerPoll * PageSize rows however deep the queue is.
            //
            //Nothing starves. Each fruitless poll advances the position, the end of the collection
            //resets it to the head, and a message that becomes eligible behind the position is found
            //on the next pass.
            _resumeAfterId = after;
            return null;
        }

        /// <summary>
        /// Whether a message can be de-queued now.
        /// </summary>
        /// <remarks>
        /// The stored times are UTC and come back as <see cref="DateTimeKind.Utc"/>, so they are
        /// compared with <c>UtcNow</c> directly. That is worth stating because LiteDB returns a
        /// plain <see cref="DateTime"/> property as <see cref="DateTimeKind.Local"/> in other
        /// shapes, and comparing one of those with <c>UtcNow</c> compares raw ticks without
        /// applying the offset - which reads a message deferred an hour ahead as ready to process.
        /// <c>DateKindIsPreserved</c> in the integration tests pins the kind so that a change in
        /// that behaviour fails a test rather than silently releasing delayed messages early.
        /// </remarks>
        private static bool IsEligible(Schema.MetaDataTable row, DateTime nowUtc, ICollection<string> routes)
        {
            if (row.Status != QueueStatuses.Waiting || row.HeartBeat != null) return false;

            if (row.QueueProcessTime.HasValue && row.QueueProcessTime.Value >= nowUtc)
                return false;

            if (row.ExpirationTime.HasValue && row.ExpirationTime.Value <= nowUtc)
                return false;

            return routes == null || routes.Contains(row.Route);
        }

        /// <summary>
        /// How many messages to examine per page. Large enough that a handful of in-flight or
        /// deferred messages at the head of the queue are absorbed by the first page, small enough
        /// that the common case - the very next message is ready - reads almost nothing.
        /// </summary>
        private const int PageSize = 64;

        /// <summary>
        /// How many pages one poll will read before giving up and resuming next time. Bounds the
        /// work a poll can do against a queue where nothing is currently eligible.
        /// </summary>
        private const int MaxPagesPerPoll = 16;

        private Tuple<Schema.QueueTable, Schema.MetaDataTable, Schema.StatusTable> DequeueRecord(ReceiveMessageQuery query, LiteDatabase db)
        {
            var col = db.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

            var record = FindNextEligible(query, col);
            if (record != null)
            {
                record.HeartBeat = DateTime.UtcNow;
                record.Status = QueueStatuses.Processing;

                col.Update(record);

                var colData = db.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                var data = colData.FindById(record.QueueId);

                if (data == null)
                {
                    //orphaned metadata record with no matching queue body - revert status
                    record.HeartBeat = null;
                    record.Status = QueueStatuses.Waiting;
                    col.Update(record);
                    return null;
                }

                Schema.StatusTable status = null;
                if (_options.Value.EnableStatusTable)
                {
                    var statusCol = db.GetCollection<Schema.StatusTable>(_tableNameHelper.StatusName);
                    var resultsStatus = statusCol.Query()
                        .Where(x => x.QueueId.Equals(record.QueueId))
                        .Limit(1)
                        .ToList();

                    if (resultsStatus.Count == 1)
                    {
                        status = resultsStatus[0];
                        status.Status = QueueStatuses.Processing;
                        statusCol.Update(status);
                    }
                }

                return new Tuple<QueueTable, MetaDataTable, StatusTable>(data, record, status);
            }

            return null;
        }
    }
}
