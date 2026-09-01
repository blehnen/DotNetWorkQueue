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
using System.Linq;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Sends a batch of messages in a single transaction.
    /// </summary>
    /// <remarks>
    /// Both the synchronous and asynchronous batch handlers delegate here, because LiteDB has no
    /// asynchronous API — the async handler wraps this on a task exactly as the single-message one
    /// does. Keeping the work in one place is also what stops the two handlers drifting apart.
    /// <para>
    /// Without this, a batch fell back to <c>SendMessages</c>'s per-message loop, which runs a
    /// <c>Parallel.ForEach</c> over single sends. Every message then paid its own connection,
    /// existence check and transaction, and the threads fanned out into LiteDB's exclusive write
    /// transaction — so batching was slower per message than sending one at a time: 211 us against
    /// 145 us. One transaction for the whole batch measures at 9.3 us per message on the same
    /// hardware, which is what this path goes after.
    /// </para>
    /// </remarks>
    internal class SendMessageCommandBatchShared
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly ISentMessageFactory _sentMessageFactory;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandBatchShared"/> class.
        /// </summary>
        public SendMessageCommandBatchShared(LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            IJobSchedulerMetaData jobSchedulerMetaData,
            ISentMessageFactory sentMessageFactory,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(connectionInformation);
            Guard.NotNull(tableNameHelper);
            Guard.NotNull(serializer);
            Guard.NotNull(optionsFactory);
            Guard.NotNull(headers);
            Guard.NotNull(jobSchedulerMetaData);
            Guard.NotNull(sentMessageFactory);
            Guard.NotNull(databaseExists);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _sentMessageFactory = sentMessageFactory;
            _databaseExists = databaseExists;
        }

        /// <summary>
        /// Sends every message in the batch, or none of them.
        /// </summary>
        /// <param name="command">The batch, in caller order.</param>
        /// <returns>One result per message, in the same order, ids included.</returns>
        /// <exception cref="NotSupportedException">The batch contains a scheduled job.</exception>
        public QueueOutputMessages Handle(SendMessageCommandBatch command)
        {
            var messages = command.Messages;
            if (messages.Count == 0)
                return new QueueOutputMessages(new List<IQueueOutputMessage>());

            GuardNoScheduledJobs(messages, _jobSchedulerMetaData);

            if (!_databaseExists.Exists())
                return new QueueOutputMessages(new List<IQueueOutputMessage>());

            var results = new IQueueOutputMessage[messages.Count];

            //One connection and one transaction for the whole batch. No lock is taken: the batch
            //path rejects scheduled jobs, so there is no check-then-act to protect, and the
            //transaction covers the inserts.
            using (var db = _connectionInformation.GetDatabase())
            {
                try
                {
                    db.Database.BeginTrans();

                    var queue = db.Database.GetCollection<QueueTable>(_tableNameHelper.QueueName);
                    var meta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);

                    for (var i = 0; i < messages.Count; i++)
                        results[i] = Write(db, queue, meta, messages[i]);

                    db.Database.Commit();
                }
                catch (Exception error)
                {
                    Rollback(db);

                    //whole-batch atomic, matching the other transports: nothing was written, so
                    //every message reports the failure rather than some of them appearing to have
                    //succeeded
                    return new QueueOutputMessages(messages
                        .Select(m => (IQueueOutputMessage)new QueueOutputMessage(
                            _sentMessageFactory.Create(null, m.MessageData.CorrelationId), error))
                        .ToList());
                }
            }

            return new QueueOutputMessages(results.ToList());
        }

        /// <summary>Writes one message's rows and returns its result.</summary>
        private IQueueOutputMessage Write(LiteDbConnection db,
            LiteDB.ILiteCollection<QueueTable> queue,
            LiteDB.ILiteCollection<MetaDataTable> meta,
            QueueMessage<IMessage, IAdditionalMessageData> message)
        {
            var serialization = _serializer.Serializer.MessageToBytes(
                new MessageBody { Body = message.Message.Body }, message.Message.Headers);

            message.Message.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph, serialization.Graph);

            var id = queue.Insert(new QueueTable
            {
                Body = serialization.Output,
                Headers = _serializer.InternalSerializer.ConvertToBytes(message.Message.Headers)
            }).AsInt32;

            var metaData = BuildMetaData(id, message);
            meta.Insert(metaData);

            if (_options.Value.EnableStatusTable)
            {
                db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName).Insert(new StatusTable
                {
                    Status = metaData.Status,
                    CorrelationId = metaData.CorrelationId,
                    QueueId = id
                });
            }

            return new QueueOutputMessage(
                _sentMessageFactory.Create(new MessageQueueId<int>(id), message.MessageData.CorrelationId));
        }

        /// <summary>
        /// Builds the meta row. No job branch: a scheduled job never reaches the batch path.
        /// </summary>
        private MetaDataTable BuildMetaData(int id, QueueMessage<IMessage, IAdditionalMessageData> message)
        {
            var metaData = new MetaDataTable
            {
                QueueId = id,
                CorrelationId = (Guid)message.MessageData.CorrelationId.Id.Value,
                QueuedDateTime = DateTime.UtcNow
            };

            if (_options.Value.EnableDelayedProcessing)
            {
                var delay = message.MessageData.GetDelay();
                if (delay.HasValue)
                    metaData.QueueProcessTime = DateTime.UtcNow.Add(delay.Value);
            }

            if (_options.Value.EnableMessageExpiration)
            {
                var expiration = message.MessageData.GetExpiration();
                if (expiration.HasValue)
                    metaData.ExpirationTime = DateTime.UtcNow.Add(expiration.Value);
            }

            if (_options.Value.EnableStatus)
                metaData.Status = QueueStatuses.Waiting;

            if (_options.Value.EnableRoute && !string.IsNullOrWhiteSpace(message.MessageData.Route))
                metaData.Route = message.MessageData.Route;

            return metaData;
        }

        /// <summary>
        /// Rolls back, swallowing a rollback failure so the original error is what the caller sees.
        /// </summary>
        private static void Rollback(LiteDbConnection db)
        {
            try
            {
                db.Database.Rollback();
            }
            catch (Exception)
            {
                //the write already failed; a failure to roll back adds nothing the caller can act on
            }
        }

        /// <summary>
        /// Rejects a batch containing a scheduled job. The batch path has no equivalent of the
        /// per-message "is this job already queued" query, and adding one would reintroduce the
        /// process-wide lock this transport just stopped taking on every send.
        /// </summary>
        /// <param name="messages">The batch.</param>
        /// <param name="jobSchedulerMetaData">Reads the job name from a message.</param>
        internal static void GuardNoScheduledJobs(
            IReadOnlyList<QueueMessage<IMessage, IAdditionalMessageData>> messages,
            IJobSchedulerMetaData jobSchedulerMetaData)
        {
            if (messages.Any(m => !string.IsNullOrWhiteSpace(jobSchedulerMetaData.GetJobName(m.MessageData))))
            {
                throw new NotSupportedException(
                    "Batch send does not support scheduled jobs; send scheduled jobs individually " +
                    "via Send(message).");
            }
        }
    }
}
