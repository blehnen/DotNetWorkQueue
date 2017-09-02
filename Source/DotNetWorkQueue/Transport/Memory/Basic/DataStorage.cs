// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <inheritdoc />
    public class DataStorage: IDataStorage
    {
        //next item to de-queue
        private static readonly ConcurrentDictionary<IConnectionInformation, ConcurrentQueue<Guid>> Queues;
       
        //actual data
        private static readonly ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>> QueueData;

        //jobs
        private static readonly ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<string, Guid>> Jobs;

        //working set
        private static readonly ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>> QueueWorking;

        //error count per queue
        private static readonly ConcurrentDictionary<IConnectionInformation, IncrementWrapper> ErrorCounts;

        //dequeue count
        private static readonly ConcurrentDictionary<IConnectionInformation, IncrementWrapper> DequeueCounts;

        private static readonly MemoryCache JobLastEventCache;

        private readonly ICompositeSerialization _serializer;
        private readonly IHeaders _headers;
        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly IMessageFactory _messageFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStorage" /> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        public DataStorage(ICompositeSerialization serializer,
            IHeaders headers,
            IJobSchedulerMetaData jobSchedulerMetaData,
            IConnectionInformation connectionInformation,
            IReceivedMessageFactory receivedMessageFactory,
            IMessageFactory messageFactory)
        {
            _serializer = serializer;
            _headers = headers;
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _connectionInformation = connectionInformation;
            _receivedMessageFactory = receivedMessageFactory;
            _messageFactory = messageFactory;

            if (!Queues.ContainsKey(_connectionInformation))
            {
                Queues.TryAdd(_connectionInformation, new ConcurrentQueue<Guid>());
            }

            if (!QueueData.ContainsKey(_connectionInformation))
            {
                QueueData.TryAdd(_connectionInformation, new ConcurrentDictionary<Guid, QueueItem>());
            }

            if (!ErrorCounts.ContainsKey(_connectionInformation))
            {
                ErrorCounts.TryAdd(_connectionInformation, new IncrementWrapper());
            }

            if (!DequeueCounts.ContainsKey(_connectionInformation))
            {
                DequeueCounts.TryAdd(_connectionInformation, new IncrementWrapper());
            }

            if (!Jobs.ContainsKey(_connectionInformation))
            {
                Jobs.TryAdd(_connectionInformation, new ConcurrentDictionary<string, Guid>());
            }

            if (!QueueWorking.ContainsKey(_connectionInformation))
            {
                QueueWorking.TryAdd(_connectionInformation, new ConcurrentDictionary<Guid, QueueItem>());
            }
        }

        static DataStorage()
        {
            Queues = new ConcurrentDictionary<IConnectionInformation, ConcurrentQueue<Guid>>();
            QueueData = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>>();
            ErrorCounts = new ConcurrentDictionary<IConnectionInformation, IncrementWrapper>();
            DequeueCounts = new ConcurrentDictionary<IConnectionInformation, IncrementWrapper>();
            Jobs = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<string, Guid>>();
            QueueWorking = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>>();
            JobLastEventCache = new MemoryCache("DataStorage");
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="inputData">The data.</param>
        /// <returns></returns>
        public Guid SendMessage(IMessage message, IAdditionalMessageData inputData)
        {
            var jobName = _jobSchedulerMetaData.GetJobName(inputData);
            var scheduledTime = DateTimeOffset.MinValue;
            var eventTime = DateTimeOffset.MinValue;
            if (!string.IsNullOrWhiteSpace(jobName))
            {
                scheduledTime = _jobSchedulerMetaData.GetScheduledTime(inputData);
                eventTime = _jobSchedulerMetaData.GetEventTime(inputData);
            }

            if (string.IsNullOrWhiteSpace(jobName) || DoesJobExist(jobName, scheduledTime) == QueueStatuses.NotQueued)
            {
                var serialization =
                    _serializer.Serializer.MessageToBytes(new MessageBody
                    {
                        Body = message.Body
                    });

                message.SetHeader(_headers.StandardHeaders.MessageInterceptorGraph,
                    serialization.Graph);
                var headers = _serializer.InternalSerializer.ConvertToBytes(message.Headers);

                var newItem = new QueueItem
                {
                    Body = serialization.Output,
                    CorrelationId = (Guid) inputData.CorrelationId.Id.Value,
                    Headers = headers,
                    Id = Guid.NewGuid(),
                    JobEventTime = eventTime,
                    JobName = jobName,
                    JobScheduledTime = scheduledTime
                };
                QueueData[_connectionInformation].TryAdd(newItem.Id, newItem);
                Queues[_connectionInformation].Enqueue(newItem.Id);

                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    Jobs[_connectionInformation].TryAdd(jobName, newItem.Id);
                }

                return newItem.Id;
            }
            throw new DotNetWorkQueueException(
                "Failed to insert record - the job has already been queued or processed");
        }

        /// <summary>
        /// Sends the message asynchronous.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public async Task<Guid> SendMessageAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            return await Task.Run(() => SendMessage(messageToSend, data)).ConfigureAwait(false);
        }
        /// <summary>
        /// Moves to error queue.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="context">The context.</param>
        public void MoveToErrorQueue(Exception exception, Guid id, IMessageContext context)
        {
            //we don't want to store all this in memory, so just keep track of the number
            Interlocked.Increment(ref ErrorCounts[_connectionInformation].ProcessedCount);

            //if we did want to store this, we would need to:

            //1) Save the work item when de-queueing it
            //2) Move the work item into an error queue on error
            //3) Delete the work item when a delete message call is received
        }
        /// <summary>
        /// Gets the next message.
        /// </summary>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public IReceivedMessageInternal GetNextMessage(List<string> routes)
        {
            if(routes != null && routes.Count > 0)
                throw new NotSupportedException("The in-memory transport does not support routes");

            if (!Queues[_connectionInformation].TryDequeue(out var id)) return null;
            if (!QueueData[_connectionInformation].TryRemove(id, out var item)) return null;

            var hasError = false;
            try
            {
                var headers = _serializer.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(item.Headers);
                var messageGraph =
                    (MessageInterceptorsGraph) headers[_headers.StandardHeaders.MessageInterceptorGraph.Name];
                var message = _serializer.Serializer.BytesToMessage<MessageBody>(item.Body, messageGraph).Body;
                var newMessage = _messageFactory.Create(message, headers);

                if (!string.IsNullOrEmpty(item.JobName))
                {
                    JobLastEventCache.Add(GenerateKey(item.JobName), item.JobEventTime, DateTimeOffset.UtcNow.AddDays(1));
                }

                Interlocked.Increment(ref DequeueCounts[_connectionInformation].ProcessedCount);

                return _receivedMessageFactory.Create(newMessage,
                    new MessageQueueId(id),
                    new MessageCorrelationId(item.CorrelationId));
            }
            catch (Exception error)
            {
                hasError = true;
                //at this point, the record has been de-queued, but it can't be processed.
                throw new PoisonMessageException(
                    "An error has occurred trying to re-assemble a message", error,
                    new MessageQueueId(id), null, null, null);

            }
            finally
            {
                if (!hasError)
                    QueueWorking[_connectionInformation].TryAdd(item.Id, item);
            }
        }
        /// <summary>
        /// Gets the next message asynchronous.
        /// </summary>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        public async Task<IReceivedMessageInternal> GetNextMessageAsync(List<string> routes)
        {
            if (routes != null && routes.Count > 0)
                throw new NotSupportedException("The in-memory transport does not support routes");

            return await Task.Run(() => GetNextMessage(routes)).ConfigureAwait(false);
        }
        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public void DeleteMessage(Guid id)
        {
            //remove data - if id is still in queue, it will fall out eventually
            QueueData[_connectionInformation].TryRemove(id, out var item);
            if(item == null)
                QueueWorking[_connectionInformation].TryRemove(id, out item);
            if (item != null)
                Jobs[_connectionInformation].TryRemove(item.JobName, out _);
        }

        /// <summary>
        /// Gets the job last known event.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        public DateTimeOffset GetJobLastKnownEvent(string jobName)
        {
            var key = GenerateKey(jobName);
            if (JobLastEventCache.Contains(key))
                return (DateTimeOffset)JobLastEventCache.Get(key);

            return default(DateTimeOffset);
        }

        /// <summary>
        /// Deletes the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        public void DeleteJob(string jobName)
        {
            if (Jobs[_connectionInformation].TryRemove(jobName, out var id))
            {
                QueueData[_connectionInformation].TryRemove(id, out _);
            }
        }
        /// <summary>
        /// Does the job exist.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        public QueueStatuses DoesJobExist(string jobName, DateTimeOffset scheduledTime)
        {
            if (Jobs[_connectionInformation].TryGetValue(jobName, out var id))
            {
                if (QueueData[_connectionInformation].TryGetValue(id, out var item))
                {
                    if (item.JobScheduledTime == scheduledTime)
                    {
                        return QueueStatuses.Waiting;
                    }
                    return QueueStatuses.Processed;
                }
                if (QueueWorking[_connectionInformation].TryGetValue(id, out item))
                {
                    if (item.JobScheduledTime == scheduledTime)
                    {
                        return QueueStatuses.Processing;
                    }
                }
            }
            return QueueStatuses.NotQueued;
        }
        /// <summary>
        /// Gets the record count.
        /// </summary>
        /// <value>
        /// The record count.
        /// </value>
        public long RecordCount => QueueData[_connectionInformation].Count;

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <returns></returns>
        public long GetErrorCount()
        {
            return Interlocked.CompareExchange(ref ErrorCounts[_connectionInformation].ProcessedCount, 0, 0);
        }

        /// <summary>
        /// Gets the dequeue count.
        /// </summary>
        /// <returns></returns>
        public long GetDequeueCount()
        {
            return Interlocked.CompareExchange(ref DequeueCounts[_connectionInformation].ProcessedCount, 0, 0);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Clear()
        {
            if (Queues.ContainsKey(_connectionInformation))
            {
                while (Queues[_connectionInformation].TryDequeue(out var id))
                {
                    QueueData[_connectionInformation].TryRemove(id, out _);
                }
                Jobs[_connectionInformation].Clear();
                QueueWorking[_connectionInformation].Clear();
            }
        }

        private string GenerateKey(string jobName)
        {
            return string.Concat(_connectionInformation.ToString(), "|", jobName);
        }

        private class IncrementWrapper
        {
            public IncrementWrapper()
            {
                ProcessedCount = 0;
            }
            public long ProcessedCount;
        }
    }
}
