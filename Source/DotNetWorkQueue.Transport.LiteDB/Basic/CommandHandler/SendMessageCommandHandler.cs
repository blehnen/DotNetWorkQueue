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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Sends a message to the queue
    /// </summary>
    internal class SendMessageCommandHandler : ICommandHandlerWithOutput<SendMessageCommand, int>
    {
        private static readonly object Locker = new object();

        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        private readonly ICompositeSerialization _serializer;
        private bool? _messageExpirationEnabled;
        private readonly IHeaders _headers;
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;
        private readonly TransportConfigurationSend _configurationSend;
        private readonly ICommandHandler<SetJobLastKnownEventCommand> _sendJobStatus;

        private readonly IQueryHandler<DoesJobExistQuery, QueueStatuses>
            _jobExistsHandler;

        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly DatabaseExists _databaseExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageCommandHandler"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="optionsFactory">The options factory.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="configurationSend">The configuration send.</param>
        /// <param name="sendJobStatus">The send job status.</param>
        /// <param name="jobExistsHandler">The job exists handler.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        /// <param name="databaseExists">The database exists.</param>
        public SendMessageCommandHandler(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper,
            ICompositeSerialization serializer,
            ILiteDbMessageQueueTransportOptionsFactory optionsFactory,
            IHeaders headers,
            TransportConfigurationSend configurationSend,
            ICommandHandler<SetJobLastKnownEventCommand> sendJobStatus,
            IQueryHandler<DoesJobExistQuery, QueueStatuses> jobExistsHandler,
            IJobSchedulerMetaData jobSchedulerMetaData,
            DatabaseExists databaseExists)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => serializer, serializer);
            Guard.NotNull(() => optionsFactory, optionsFactory);
            Guard.NotNull(() => headers, headers);
            Guard.NotNull(() => configurationSend, configurationSend);
            Guard.NotNull(() => sendJobStatus, sendJobStatus);
            Guard.NotNull(() => jobExistsHandler, jobExistsHandler);
            Guard.NotNull(() => databaseExists, databaseExists);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
            _serializer = serializer;
            _options = new Lazy<LiteDbMessageQueueTransportOptions>(optionsFactory.Create);
            _headers = headers;
            _configurationSend = configurationSend;
            _sendJobStatus = sendJobStatus;
            _jobExistsHandler = jobExistsHandler;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _databaseExists = databaseExists;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="commandSend">The command.</param>
        /// <returns></returns>
        public int Handle(SendMessageCommand commandSend)
        {
            if (!_databaseExists.Exists())
            {
                return 0;
            }

            if (!_messageExpirationEnabled.HasValue)
            {
                _messageExpirationEnabled = _options.Value.EnableMessageExpiration;
            }

            TimeSpan? expiration = null;
            if (_messageExpirationEnabled.Value)
            {
                expiration = MessageExpiration.GetExpiration(commandSend, data => data.GetExpiration());
            }

            var jobName = _jobSchedulerMetaData.GetJobName(commandSend.MessageData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(commandSend.MessageData);
                eventTime = _jobSchedulerMetaData.GetEventTime(commandSend.MessageData);
            }

            var id = 0;
            using (var db = _connectionInformation.GetDatabase())
            {
                lock (Locker) //we need to block due to jobs
                {
                    try
                    {
                        db.Database.BeginTrans(); //only blocks on shared connections
                        if (string.IsNullOrWhiteSpace(jobName) || _jobExistsHandler.Handle(
                                new DoesJobExistQuery(jobName, scheduledTime, db.Database)) ==
                            QueueStatuses.NotQueued)
                        {
                            var serialization =
                                _serializer.Serializer.MessageToBytes(
                                    new MessageBody {Body = commandSend.MessageToSend.Body},
                                    commandSend.MessageToSend.Headers);

                            //create queue
                            var queueData = new QueueTable() {Body = serialization.Output};
                            commandSend.MessageToSend.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph,
                                serialization.Graph);
                            queueData.Headers =
                                _serializer.InternalSerializer.ConvertToBytes(commandSend.MessageToSend.Headers);

                            var col = db.Database.GetCollection<QueueTable>(_tableNameHelper.QueueName);
                            id = col.Insert(queueData).AsInt32;

                            //create metadata
                            var metaData = new MetaDataTable
                            {
                                QueueId = id,
                                CorrelationId = (Guid) commandSend.MessageData.CorrelationId.Id.Value,
                                QueuedDateTime = DateTime.UtcNow
                            };

                            if (!string.IsNullOrWhiteSpace(jobName))
                            {
                                metaData.QueueProcessTime = scheduledTime.UtcDateTime;
                            }
                            else if (_options.Value.EnableDelayedProcessing)
                            {
                                var delay = commandSend.MessageData.GetDelay();
                                if (delay.HasValue)
                                {
                                    metaData.QueueProcessTime = DateTime.UtcNow.Add(delay.Value);
                                }
                            }

                            if (_options.Value.EnableMessageExpiration && expiration.HasValue)
                                metaData.ExpirationTime = DateTime.UtcNow.Add(expiration.Value);

                            if (_options.Value.EnableStatus)
                                metaData.Status = QueueStatuses.Waiting;

                            if (_options.Value.EnableRoute && !string.IsNullOrWhiteSpace(commandSend.MessageData.Route))
                            {
                                metaData.Route = commandSend.MessageData.Route;
                            }

                            var colMeta = db.Database.GetCollection<MetaDataTable>(_tableNameHelper.MetaDataName);
                            colMeta.Insert(metaData);

                            //create status table record
                            if (_options.Value.EnableStatusTable || !string.IsNullOrWhiteSpace(jobName))
                            {
                                var statusData = new StatusTable()
                                {
                                    Status = metaData.Status, CorrelationId = metaData.CorrelationId, QueueId = id
                                };

                                if (!string.IsNullOrWhiteSpace(jobName))
                                    statusData.JobName = jobName;

                                var colStatus = db.Database.GetCollection<StatusTable>(_tableNameHelper.StatusName);
                                colStatus.Insert(statusData);
                            }

                            //job name
                            if (!string.IsNullOrWhiteSpace(jobName))
                            {
                                _sendJobStatus.Handle(new SetJobLastKnownEventCommand(jobName, eventTime,
                                    scheduledTime, db.Database));
                            }
                        }
                        else
                        {
                            throw new DotNetWorkQueueException(
                                "Failed to insert record - the job has already been queued or processed");
                        }

                        db.Database.Commit();
                    }
                    catch
                    {
                        db.Database.Rollback();
                        throw;
                    }
                }

                return id;
            }
        }
    }
}
