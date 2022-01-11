﻿// ---------------------------------------------------------------------
// Copyright © 2015-2020 Brian Lehnen
// 
// All rights reserved.
// 
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using ExampleMessage;
using Microsoft.Extensions.Logging;

namespace ConsoleSharedCommands.Commands
{
    public abstract class ConsumeMessageAsync<TTransportInit> : SharedCommands
        where TTransportInit: class, ITransportInit, new()
    {
        private readonly Lazy<QueueContainer<TTransportInit>> _queueContainer;
        private readonly Lazy<SchedulerContainer> _schedulerContainer; 
        protected readonly Dictionary<string, IConsumerBaseQueue> Queues;
        private ATaskScheduler _taskScheduler;
        private ITaskFactory _taskFactory;

        protected ConsumeMessageAsync()
        {
            _schedulerContainer = new Lazy<SchedulerContainer>(CreateSchedulerContainer);
            _queueContainer = new Lazy<QueueContainer<TTransportInit>>(CreateContainer);
            Queues = new Dictionary<string, IConsumerBaseQueue>();
        }

        public override ConsoleExecuteResult Help()
        {

            var help = new StringBuilder();
            help.AppendLine(base.Help().Message);

            help.AppendLine(ConsoleFormatting.FixedLength("SetTaskSchedulerConfiguration",
                "Task scheduler configuration options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetWorkerConfiguration queueName",
                "Worker configuration options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetHeartBeatConfiguration queueName",
                "HeartBeat configuration options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetMessageExpirationConfiguration queueName",
                "Message Expiration configuration options"));

            help.AppendLine(ConsoleFormatting.FixedLength("SetFatalExceptionDelayBehavior queueName",
                "Back off times for when fatal errors occur"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetQueueDelayBehavior queueName",
                "Back off times for when the queue is empty"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetQueueRetryBehavior queueName",
                "Retry strategy, based on the type of the exception"));

            help.AppendLine(ConsoleFormatting.FixedLength("CreateQueue queueName queueType",
              "Creates the initial queue in memory. 0=POCO, 1=Linq Expression"));
            help.AppendLine(ConsoleFormatting.FixedLength("StartQueue queueName", "Starts a queue"));
            help.AppendLine(ConsoleFormatting.FixedLength("StopQueue queueName",
                "Stops a queue; configuration will be reset"));
            return new ConsoleExecuteResult(help.ToString());
        }

        public override ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "SetTaskSchedulerConfiguration":
                    return new ConsoleExecuteResult("SetTaskSchedulerConfiguration 4 0 1 00:00:30 00:00:05");
                case "SetWorkerConfiguration":
                    return new ConsoleExecuteResult("SetWorkerConfiguration examplequeue 5 true false 00:00:05 00:00:5");
                case "SetHeartBeatConfiguration":
                    return new ConsoleExecuteResult("SetHeartBeatConfiguration examplequeue 2 00:00:30 00:00:10 1 1 00:01:00");
                case "SetMessageExpirationConfiguration":
                    return new ConsoleExecuteResult("SetMessageExpirationConfiguration examplequeue true 00:01:00");
                case "SetFatalExceptionDelayBehavior":
                    return new ConsoleExecuteResult("SetFatalExceptionDelayBehavior examplequeue 00:00:01,00:00:05,00:00:30");
                case "SetQueueDelayBehavior":
                    return new ConsoleExecuteResult("SetQueueDelayBehavior examplequeue 00:00:01,00:00:02,00:00:03");
                case "SetQueueRetryBehavior":
                    return new ConsoleExecuteResult("SetQueueRetryBehavior examplequeue System.TimeoutException 00:00:01,00:00:02,00:00:03");

                case "StartQueue":
                    return new ConsoleExecuteResult("StartQueue examplequeue");
                case "StopQueue":
                    return new ConsoleExecuteResult("StopQueue examplequeue");
                case "CreateQueue":
                    return new ConsoleExecuteResult("CreateQueue examplequeue 0");
            }
            return base.Example(command);
        }

        protected override ConsoleExecuteResult ValidateQueue(string queueName)
        {
            if (!Queues.ContainsKey(queueName)) return new ConsoleExecuteResult($"{queueName} was not found. Call CreateQueue to create the queue first");
            return null;
        }

        public ConsoleExecuteResult CreateQueue(QueueConnection queueConnection, int type)
        {
            if (Enum.IsDefined(typeof(ConsumerQueueTypes), type))
            {
                CreateModuleIfNeeded(queueConnection, (ConsumerQueueTypes)type);
                return new ConsoleExecuteResult($"{queueConnection.Queue} has been created");
            }
            return new ConsoleExecuteResult($"Invalid queue type {type}. Valid values are 0=POCO,1=Linq Expression");
        }

        public ConsoleExecuteResult SetFatalExceptionDelayBehavior(string queueName, params TimeSpan[] timespan)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            Queues[queueName].Configuration.TransportConfiguration.FatalExceptionDelayBehavior.Clear();
            Queues[queueName].Configuration.TransportConfiguration.FatalExceptionDelayBehavior.Add(timespan.ToList());
            return new ConsoleExecuteResult("fatal exception delays have been set");
        }

        public ConsoleExecuteResult SetQueueDelayBehavior(string queueName, params TimeSpan[] timespan)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            Queues[queueName].Configuration.TransportConfiguration.QueueDelayBehavior.Clear();
            Queues[queueName].Configuration.TransportConfiguration.QueueDelayBehavior.Add(timespan.ToList());
            return new ConsoleExecuteResult("queue delays have been set");
        }

        public ConsoleExecuteResult SetQueueRetryBehavior(string queueName, string exceptionType, params TimeSpan[] timespan)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            Queues[queueName].Configuration.TransportConfiguration.RetryDelayBehavior.Add(Type.GetType(exceptionType, true), timespan.ToList());
            return new ConsoleExecuteResult("queue delays have been set");
        }

        public ConsoleExecuteResult SetTaskSchedulerConfiguration(int maximumThreads,
            int maxQueueSize = 0,
            TimeSpan? waitForThreadPoolToFinish = null
            )
        {
            CreateModuleIfNeeded(new QueueConnection(string.Empty, string.Empty));

            _taskScheduler.Configuration.MaximumThreads = maximumThreads;
            _taskScheduler.Configuration.MaxQueueSize = maxQueueSize;
            if (waitForThreadPoolToFinish.HasValue)
            {
                _taskScheduler.Configuration.WaitForThreadPoolToFinish = waitForThreadPoolToFinish.Value;
            }

            return new ConsoleExecuteResult("task scheduler configuration set");
        }
        public ConsoleExecuteResult SetWorkerConfiguration(string queueName, 
            int workerCount = 1,
            bool singleWorkerWhenNoWorkFound = true,
            bool abortWorkerThreadsWhenStopping = false,
            TimeSpan? timeToWaitForWorkersToStop = null,
            TimeSpan? timeToWaitForWorkersToCancel = null
            )
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            Queues[queueName].Configuration.Worker.WorkerCount = workerCount;
            Queues[queueName].Configuration.Worker.SingleWorkerWhenNoWorkFound = singleWorkerWhenNoWorkFound;
            Queues[queueName].Configuration.Worker.AbortWorkerThreadsWhenStopping = abortWorkerThreadsWhenStopping;
            if (timeToWaitForWorkersToCancel.HasValue)
            {
                Queues[queueName].Configuration.Worker.TimeToWaitForWorkersToCancel =
                    timeToWaitForWorkersToCancel.Value;
            }
            if (timeToWaitForWorkersToStop.HasValue)
            {
                Queues[queueName].Configuration.Worker.TimeToWaitForWorkersToStop =
                    timeToWaitForWorkersToStop.Value;
            }

            return new ConsoleExecuteResult($"worker configuration set for {queueName}");
        }

        public ConsoleExecuteResult SetHeartBeatConfiguration(string queueName, 
            string updateTime = "min(*%1)",
            TimeSpan? monitorTime = null,
            TimeSpan? deadTime = null,
            int heartbeatThreadsMax = 1
            )
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            Queues[queueName].Configuration.HeartBeat.UpdateTime = updateTime;
            if (deadTime.HasValue)
            {
                Queues[queueName].Configuration.HeartBeat.Time = deadTime.Value;
            }
            if (monitorTime.HasValue)
            {
                Queues[queueName].Configuration.HeartBeat.MonitorTime = monitorTime.Value;
            }
            Queues[queueName].Configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMax = heartbeatThreadsMax;

            return new ConsoleExecuteResult($"heartbeat configuration set for {queueName}");
        }

        public ConsoleExecuteResult SetMessageExpirationConfiguration(string queueName,
            bool enabled = true,
            TimeSpan? monitorTime = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            Queues[queueName].Configuration.MessageExpiration.Enabled = enabled;
            if (monitorTime.HasValue)
            {
                Queues[queueName].Configuration.MessageExpiration.MonitorTime = monitorTime.Value;
            }
            return new ConsoleExecuteResult($"message expiration configuration set for {queueName}");
        }

        public ConsoleExecuteResult AddWorkGroup(QueueConnection queueConnection, ConsumerQueueTypes type, string workGroupName, int concurrencyLevel, int maxQueueSize = 0)
        {
            CreateModuleIfNeeded(queueConnection, type, workGroupName, concurrencyLevel, maxQueueSize);
            return new ConsoleExecuteResult($"Added workgroup {workGroupName} for queue {queueConnection.Queue}");
        }

        public ConsoleExecuteResult StopQueue(string queueName)
        {
            if(Queues.ContainsKey(queueName))
            {
                Queues[queueName].Dispose();
                Queues.Remove(queueName);
                return new ConsoleExecuteResult($"{queueName} has been stopped");
            }
            return new ConsoleExecuteResult($"{queueName} was not found");
        }

        public ConsoleExecuteResult StartQueue(string queueName)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            if (!_taskScheduler.Started)
            {
                _taskScheduler.Start();
            }

            //start looking for work
            var queue = Queues[queueName];
            var consumerQueue = queue as IConsumerQueueScheduler;
            consumerQueue?.Start<SimpleMessage>(HandleMessages);

            var consumerMethodQueue = queue as IConsumerMethodQueueScheduler;
            consumerMethodQueue?.Start();

            return new ConsoleExecuteResult($"{queueName} started");
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var queue in Queues.Values)
            {
                queue.Dispose();
            }
            Queues.Clear();
            if (_queueContainer.IsValueCreated)
            {
                _queueContainer.Value.Dispose();
            }

            _taskScheduler?.Dispose();
            if (_schedulerContainer.IsValueCreated)
            {
                _schedulerContainer.Value.Dispose();
            }
            base.Dispose(disposing);
        }

        protected SchedulerContainer CreateSchedulerContainer()
        {
            return new SchedulerContainer(RegisterSchedulerService);
        }
        protected QueueContainer<TTransportInit> CreateContainer()
        {
            return new QueueContainer<TTransportInit>(RegisterService);
        }

        private void RegisterSchedulerService(IContainer container)
        {
            if (Metrics != null)
            {
                container.Register<IMetrics>(() => Metrics, LifeStyles.Singleton);
            }
        }

        private void RegisterService(IContainer container)
        {
            if (Metrics != null)
            {
                container.Register<IMetrics>(() => Metrics, LifeStyles.Singleton);
            }

            if (Des)
            {
                container.Register(() => DesConfiguration,
                    LifeStyles.Singleton);
            }
        }

        protected void CreateModuleIfNeeded(QueueConnection queueConnection, ConsumerQueueTypes type = ConsumerQueueTypes.Poco, string workGroupName = null, int concurrencyLevel = 0, int maxQueueSize = 0)
        {
            if (_taskScheduler == null)
            {
                _taskScheduler = _schedulerContainer.Value.CreateTaskScheduler();
                _taskFactory = _schedulerContainer.Value.CreateTaskFactory(_taskScheduler);
            }

            if (!string.IsNullOrWhiteSpace(queueConnection.Queue) && !Queues.ContainsKey(queueConnection.Queue))
            {
                IConsumerBaseQueue queue = null;
                if (workGroupName != null)
                {
                    if (!_taskScheduler.Started)
                    {
                        _taskScheduler.Start();
                    }

                    var group = _taskScheduler.AddWorkGroup(workGroupName, concurrencyLevel, maxQueueSize);
                    switch (type)
                    {
                        case ConsumerQueueTypes.Poco:
                            queue = _queueContainer.Value.CreateConsumerQueueScheduler(queueConnection, _taskFactory, group);
                            break;
                        case ConsumerQueueTypes.Method:
                            queue = _queueContainer.Value.CreateConsumerMethodQueueScheduler(queueConnection, _taskFactory, group);
                            break;
                    }
                }
                else
                {
                    switch (type)
                    {
                        case ConsumerQueueTypes.Poco:
                            queue = _queueContainer.Value.CreateConsumerQueueScheduler(queueConnection, _taskFactory);
                            break;
                        case ConsumerQueueTypes.Method:
                            queue = _queueContainer.Value.CreateConsumerMethodQueueScheduler(queueConnection, _taskFactory);
                            break;
                    }
                }

                Queues.Add(queueConnection.Queue, queue);
            }
        }

        /// <summary>
        /// Handles the messages.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="notifications">The notifications.</param>
        private void HandleMessages(IReceivedMessage<SimpleMessage> message, IWorkerNotification notifications)
        {
            notifications.Log.LogDebug(
                $"Processing Message {message.MessageId} with run time {message.Body.RunTimeInMs}");

            if (message.Body.RunTimeInMs > 0)
            {
                var end = DateTime.Now + TimeSpan.FromMilliseconds(message.Body.RunTimeInMs);
                if (notifications.TransportSupportsRollback)
                {
                    Task.Delay(message.Body.RunTimeInMs, notifications.WorkerStopping.CancelWorkToken).Wait(notifications.WorkerStopping.CancelWorkToken);
                }
                else //no rollback possible; we will ignore cancel / stop requests
                {
                    Task.Delay(message.Body.RunTimeInMs);
                }

                if (DateTime.Now < end) //did we finish?
                { //nope - we probably are being canceled
                    if (notifications.TransportSupportsRollback && notifications.WorkerStopping.CancelWorkToken.IsCancellationRequested)
                    {
                        notifications.Log.LogDebug("Cancel has been requested - aborting");
                        notifications.WorkerStopping.CancelWorkToken.ThrowIfCancellationRequested();
                    }
                }
            }
            notifications.Log.LogDebug($"Processed message {message.MessageId}");
        }
    }
}
