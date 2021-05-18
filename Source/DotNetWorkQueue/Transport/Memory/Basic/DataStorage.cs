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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Storage for memory transport; all active messages are contained here
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.Memory.IDataStorage" />
    /// <seealso cref="DotNetWorkQueue.Transport.Memory.IDataStorageSendMessage" />
    public class DataStorage: IDataStorage, IDataStorageSendMessage
    {
        //next item to de-queue
        private static readonly ConcurrentDictionary<IConnectionInformation, BlockingCollection<Guid>> Queues;
       
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

        private static readonly CachePolicy<DateTimeOffset> JobLastEventCache;

        private readonly IJobSchedulerMetaData _jobSchedulerMetaData;
        private readonly IConnectionInformation _connectionInformation;
        private readonly IReceivedMessageFactory _receivedMessageFactory;
        private readonly IMessageFactory _messageFactory;
        private readonly IQueueCancelWork _cancelToken;

        private bool _complete;

        /// <summary>Initializes a new instance of the <see cref="DataStorage" /> class.</summary>
        /// <param name="jobSchedulerMetaData">The job scheduler meta data.</param>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="receivedMessageFactory">The received message factory.</param>
        /// <param name="messageFactory">The message factory.</param>
        /// <param name="cancelToken">cancel token for stopping</param>
        public DataStorage(
            IJobSchedulerMetaData jobSchedulerMetaData,
            IConnectionInformation connectionInformation,
            IReceivedMessageFactory receivedMessageFactory,
            IMessageFactory messageFactory,
            IQueueCancelWork cancelToken)
        {
            _jobSchedulerMetaData = jobSchedulerMetaData;
            _connectionInformation = connectionInformation;
            _receivedMessageFactory = receivedMessageFactory;
            _messageFactory = messageFactory;
            _cancelToken = cancelToken;

            if (!Queues.ContainsKey(_connectionInformation))
            {
                Queues.TryAdd(_connectionInformation, new BlockingCollection<Guid>());
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

            _complete = false;
        }

        /// <summary>
        /// Initializes the <see cref="DataStorage"/> class.
        /// </summary>
        static DataStorage()
        {
            Queues = new ConcurrentDictionary<IConnectionInformation, BlockingCollection<Guid>>();
            QueueData = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>>();
            ErrorCounts = new ConcurrentDictionary<IConnectionInformation, IncrementWrapper>();
            DequeueCounts = new ConcurrentDictionary<IConnectionInformation, IncrementWrapper>();
            Jobs = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<string, Guid>>();
            QueueWorking = new ConcurrentDictionary<IConnectionInformation, ConcurrentDictionary<Guid, QueueItem>>();

            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var memoryCacheProvider = new MemoryCacheProvider(memoryCache);

            JobLastEventCache = Policy.Cache<DateTimeOffset>(cacheProvider: memoryCacheProvider, ttlStrategy: new JobTimeStrategy(),
                cacheKeyStrategy: new CacheKeyStrategy(), onCacheError: (context, s, arg3) => { });
        }

        /// <inheritdoc />
        public Guid SendMessage(IMessage message, IAdditionalMessageData inputData)
        {
            if (Queues[_connectionInformation].IsAddingCompleted)
                return Guid.Empty;

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
                var newItem = new QueueItem
                {
                    Body = message.Body,
                    CorrelationId = (Guid) inputData.CorrelationId.Id.Value,
                    Headers = message.Headers,
                    Id = Guid.NewGuid(),
                    JobEventTime = eventTime,
                    JobName = jobName,
                    JobScheduledTime = scheduledTime
                };
                QueueData[_connectionInformation].TryAdd(newItem.Id, newItem);
                Queues[_connectionInformation].Add(newItem.Id);

                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    Jobs[_connectionInformation].TryAdd(jobName, newItem.Id);
                }
                return newItem.Id;
            }
            throw new DotNetWorkQueueException(
                "Failed to insert record - the job has already been queued or processed");
        }

        /// <inheritdoc />
        public async Task<Guid> SendMessageAsync(IMessage messageToSend, IAdditionalMessageData data)
        {
            return await Task.Run(() => SendMessage(messageToSend, data)).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public void MoveToErrorQueue(Exception exception, Guid id, IMessageContext context)
        {
            //we don't want to store all this in memory, so just keep track of the number
            Interlocked.Increment(ref ErrorCounts[_connectionInformation].ProcessedCount);

            //if we did want to store this, we would need to:

            //1) Save the work item when de-queueing it
            //2) Move the work item into an error queue on error
            //3) Delete the work item when a delete message call is received
        }
        /// <inheritdoc />
        public IReceivedMessageInternal GetNextMessage(List<string> routes, TimeSpan timeout)
        {
            if (_complete)
                return null;

            if(routes != null && routes.Count > 0)
                throw new NotSupportedException("The in-memory transport does not support routes");

            using (CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(_cancelToken.CancelWorkToken,
                    _cancelToken.StopWorkToken))
            {
                Guid id = Guid.Empty;
                try
                {
                    if (!Queues[_connectionInformation].TryTake(out id, Convert.ToInt32(timeout.TotalMilliseconds), linkedCts.Token))
                    {
                        return null;
                    }
                }
                catch (OperationCanceledException)
                {
                    return null;
                }

                if (!QueueData[_connectionInformation].TryRemove(id, out var item))
                {
                    return null;
                }

                var hasError = false;
                try
                {
                    var newMessage = _messageFactory.Create(item.Body, item.Headers);

                    if (!string.IsNullOrEmpty(item.JobName))
                    {
                        var key = GenerateKey(item.JobName);

                        //add it to the cache
                        JobLastEventCache.Execute(context => item.JobEventTime, new Context(key));
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
        }
        /// <summary>
        /// Gets the headers for the specified message if possible
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// null if the headers could not be obtained; otherwise a collection with 0 or more records
        /// </returns>
        public IDictionary<string, object> GetHeaders(Guid id)
        {
            return !QueueData[_connectionInformation].TryGetValue(id, out var item) ? null : item.Headers;
        }
        /// <inheritdoc />
        public bool DeleteMessage(Guid id)
        {
            //remove data - if id is still in queue, it will fall out eventually
            var removed = QueueData[_connectionInformation].TryRemove(id, out var item);
            if(item == null)
                removed = QueueWorking[_connectionInformation].TryRemove(id, out item);

            if (item != null)
                Jobs[_connectionInformation].TryRemove(item.JobName, out _);

            return removed;
        }

        /// <inheritdoc />
        public DateTimeOffset GetJobLastKnownEvent(string jobName)
        {
            return JobLastEventCache.Execute(context => default, new Context(GenerateKey(jobName)));
        }

        /// <inheritdoc />
        public void DeleteJob(string jobName)
        {
            if (Jobs[_connectionInformation].TryRemove(jobName, out var id))
            {
                QueueData[_connectionInformation].TryRemove(id, out _);
            }
        }

        /// <inheritdoc />
        public QueueStatuses DoesJobExist(string jobName, DateTimeOffset scheduledTime)
        {
            if (Jobs[_connectionInformation].TryGetValue(jobName, out var id))
            {
                if (QueueData[_connectionInformation].TryGetValue(id, out _))
                {
                    return QueueStatuses.Waiting;
                }
                if (QueueWorking[_connectionInformation].TryGetValue(id, out _))
                {
                    return QueueStatuses.Processing;
                }
            }
            var time = GetJobLastKnownEvent(jobName);
            return time == scheduledTime ? QueueStatuses.Processed : QueueStatuses.NotQueued;
        }

        /// <inheritdoc />
        public long RecordCount => QueueData[_connectionInformation].Count;

        /// <inheritdoc />
        public long GetErrorCount()
        {
            return Interlocked.CompareExchange(ref ErrorCounts[_connectionInformation].ProcessedCount, 0, 0);
        }

        /// <inheritdoc />
        public long GetDequeueCount()
        {
            return Interlocked.CompareExchange(ref DequeueCounts[_connectionInformation].ProcessedCount, 0, 0);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _complete = true;
            if (Queues.ContainsKey(_connectionInformation))
            {
                if (!Queues[_connectionInformation].IsAddingCompleted)
                {
                    Queues[_connectionInformation].CompleteAdding();
                    while (Queues[_connectionInformation].TryTake(out var id))
                    {
                        QueueData[_connectionInformation].TryRemove(id, out _);
                    }

                    Jobs[_connectionInformation].Clear();
                    QueueWorking[_connectionInformation].Clear();
                }
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

        private class JobTimeStrategy : ITtlStrategy
        {
            public Ttl GetTtl(Context context, object result)
            {
                switch (result)
                {
                    case null:
                        return new Ttl(TimeSpan.Zero, false);
                    case DateTimeOffset cast:
                        return cast == default ? new Ttl(TimeSpan.Zero, false) : new Ttl(TimeSpan.FromDays(1), false);
                }
                return new Ttl(TimeSpan.Zero, false);
            }
        }

        private class CacheKeyStrategy : ICacheKeyStrategy
        {
            public string GetCacheKey(Context context)
            {
                return context.OperationKey;
            }
        }
    }
}
