// ---------------------------------------------------------------------
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
using ConsoleSharedCommands.Commands;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis;
using DotNetWorkQueue.Transport.Redis.Basic;
using ExampleMessage;

namespace RedisProducer.Commands
{
    public class SendMessage : SharedSendCommands
    {
        private readonly Lazy<QueueContainer<RedisQueueInit>> _queueContainer;
        private readonly Dictionary<string, IProducerBaseQueue> _queues;

        private readonly object _asyncStringBuilderLock = new object();

        public SendMessage()
        {
            _queueContainer = new Lazy<QueueContainer<RedisQueueInit>>(CreateContainer);
            _queues = new Dictionary<string, IProducerBaseQueue>();
        }

        protected override Dictionary<string, IProducerBaseQueue> Queues => _queues;

        private QueueContainer<RedisQueueInit> CreateContainer()
        {
            return new QueueContainer<RedisQueueInit>(RegisterService);
        }

        private void RegisterService(IContainer container)
        {
            if (Metrics != null)
            {
                container.Register<IMetrics>(() => Metrics, LifeStyles.Singleton);
            }

            if (Des && Gzip)
            {
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (GZipMessageInterceptor), //gzip compression
                    typeof (TripleDesMessageInterceptor) //encryption
                });
                container.Register(() => DesConfiguration, LifeStyles.Singleton);
            }
            else if (Gzip)
            {
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (GZipMessageInterceptor) //gzip compression
                });
            }
            else if (Des)
            {
                container.RegisterCollection<IMessageInterceptor>(new[]
                {
                    typeof (TripleDesMessageInterceptor) //encryption
                });
                container.Register(() => DesConfiguration,
                    LifeStyles.Singleton);
            }
        }

        public override ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("SendMessage", "Sends messages to a queue"));

        public override ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.Append(base.Help().Message);
            help.AppendLine(ConsoleFormatting.FixedLength("SetRedisOptions queueName", "Sets redis transport specific options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetTimeClientOptions queueName", "Options for obtaining the current time. 0=localmachine,1=redisServer,2=NTP"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetMessageIdOptions queueName", "Message ID generation. 0=redis,1=uuid"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetSntpTimeConfiguration queueName", "Options for the NTP client"));
            help.AppendLine("-The following commands send messages-");
            help.AppendLine(ConsoleFormatting.FixedLength("Send queueName", "Sends messages"));
            help.AppendLine(ConsoleFormatting.FixedLength("SendAsync queueName", "Sends messages async"));
            return new ConsoleExecuteResult(help.ToString());
        }

        public override ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "SetRedisOptions":
                    return new ConsoleExecuteResult("SetRedisOptions examplequeue 10 10 10 00:00:01");
                case "SetTimeClientOptions":
                    return new ConsoleExecuteResult("SetTimeClientOptions examplequeue 1");
                case "SetMessageIdOptions":
                    return new ConsoleExecuteResult("SetMessageIdOptions examplequeue 1");
                case "SetSntpTimeConfiguration":
                    return new ConsoleExecuteResult("SetSntpTimeConfiguration 123 pool.ntp.org 00:00:08");
                case "Send":
                    return new ConsoleExecuteResult("Send examplequeue 10 100 true null null");
                case "SendAsync":
                    return new ConsoleExecuteResult("SendAsync examplequeue 10 100 true null null");
            }
            return base.Example(command);
        }

        protected override ConsoleExecuteResult ValidateQueue(string queueName)
        {
            if (!_queues.ContainsKey(queueName)) return new ConsoleExecuteResult($"{queueName} was not found. Call CreateQueue to create the queue first");
            return null;
        }

        public ConsoleExecuteResult SetRedisOptions(string queueName,
           int clearExpiredMessagesBatchLimit = 50,
           int moveDelayedMessagesBatchLimit = 50,
           int resetHeartBeatBatchLimit = 50,
           TimeSpan? delayedProcessingMonitorTime = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            _queues[queueName].Configuration.Options().ClearExpiredMessagesBatchLimit = clearExpiredMessagesBatchLimit;
            if (delayedProcessingMonitorTime.HasValue)
            {
                _queues[queueName].Configuration.Options().DelayedProcessingConfiguration.MonitorTime = delayedProcessingMonitorTime.Value;
            }
            _queues[queueName].Configuration.Options().MoveDelayedMessagesBatchLimit = moveDelayedMessagesBatchLimit;
            _queues[queueName].Configuration.Options().ResetHeartBeatBatchLimit = resetHeartBeatBatchLimit;
            return new ConsoleExecuteResult("options set");
        }

        public ConsoleExecuteResult SetSntpTimeConfiguration(string queueName,
            int port = 123,
            string server = "pool.ntp.org",
            TimeSpan? timeout = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            _queues[queueName].Configuration.Options().SntpTimeConfiguration.Port = port;
            _queues[queueName].Configuration.Options().SntpTimeConfiguration.Server = server;
            if (timeout.HasValue)
            {
                _queues[queueName].Configuration.Options().SntpTimeConfiguration.TimeOut = timeout.Value;
            }
            return new ConsoleExecuteResult("options set");
        }

        public ConsoleExecuteResult SetTimeClientOptions(string queueName, int value)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            if (Enum.IsDefined(typeof(TimeLocations), value))
            {
                var type = (TimeLocations)value;
                _queues[queueName].Configuration.Options().TimeServer = type;
                return new ConsoleExecuteResult($"Set time client to {type}");
            }
            return new ConsoleExecuteResult($"invalid value {value}");
        }

        public ConsoleExecuteResult SetMessageIdOptions(string queueName, int value)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            if (Enum.IsDefined(typeof(MessageIdLocations), value))
            {
                var type = (MessageIdLocations)value;
                _queues[queueName].Configuration.Options().MessageIdLocation = type;
                return new ConsoleExecuteResult($"Set time client to {type}");
            }
            return new ConsoleExecuteResult($"invalid value {value}");
        }

        public ConsoleExecuteResult Send(string queueName,
            int itemCount,
            int runtime = 100,
            bool batched = false,
            TimeSpan? delay = null,
            TimeSpan? expiration = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            var returnMessage = new StringBuilder();
            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration);
            if (batched)
            {
                var result = ((IProducerQueue<SimpleMessage>)_queues[queueName]).Send(messages);
                if (result.HasErrors)
                {
                    foreach (var error in result.Where(error => error.HasError))
                    {
                        returnMessage.AppendLine(error.SendingException.ToString());
                    }
                }
            }
            else
            {
                foreach (var message in messages)
                {
                    var result = ((IProducerQueue<SimpleMessage>)_queues[queueName]).Send(message.Message, message.MessageData);
                    if (result.HasError)
                    {
                        returnMessage.AppendLine(result.SendingException.ToString());
                    }
                }
            }

            if (returnMessage.Length == 0)
            {
                returnMessage.AppendLine($"Sent {itemCount} messages");
            }

            return new ConsoleExecuteResult(returnMessage.ToString());
        }

        public async Task<ConsoleExecuteResult> SendAsync(string queueName,
            int itemCount,
            int runtime = 100,
            bool batched = false,
            TimeSpan? delay = null,
            TimeSpan? expiration = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            var returnMessage = new StringBuilder();
            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration);
            if (batched)
            {
                var result = await ((IProducerQueue<SimpleMessage>)_queues[queueName]).SendAsync(messages).ConfigureAwait(false);
                if (result.HasErrors)
                {
                    foreach (var error in result.Where(error => error.HasError))
                    {
                        lock (_asyncStringBuilderLock)
                        {
                            returnMessage.AppendLine(error.SendingException.ToString());
                        }
                    }
                }
            }
            else
            {
                foreach (var message in messages)
                {
                    var result = await ((IProducerQueue<SimpleMessage>)_queues[queueName]).SendAsync(message.Message, message.MessageData).ConfigureAwait(false);
                    if (!result.HasError) continue;
                    lock (_asyncStringBuilderLock)
                    {
                        returnMessage.AppendLine(result.SendingException.ToString());
                    }
                }
            }

            lock (_asyncStringBuilderLock)
            {
                if (returnMessage.Length == 0)
                {
                    returnMessage.AppendLine($"Sent {itemCount} messages");
                }
            }

            return new ConsoleExecuteResult(returnMessage.ToString());
        }
        protected override void Dispose(bool disposing)
        {
            foreach (var queue in _queues.Values)
            {
                queue.Dispose();
            }
            _queues.Clear();
            if (_queueContainer.IsValueCreated)
            {
                _queueContainer.Value.Dispose();
            }
            base.Dispose(disposing);
        }

        private IEnumerable<SimpleMessage> CreateMessages(int itemCount, int runTime)
        {
            return Enumerable.Range(0, itemCount)
                .Select(
                    i =>
                        new SimpleMessage
                        {
                            Message = DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            RunTimeInMs = runTime
                        });
        }

        private List<QueueMessage<SimpleMessage, IAdditionalMessageData>> GenerateMessages(List<SimpleMessage> jobs,
            TimeSpan? delay = null,
            TimeSpan? expiration = null
            )
        {
            var messages = new List<QueueMessage<SimpleMessage, IAdditionalMessageData>>(jobs.Count);
            foreach (var message in jobs)
            {
                if (delay.HasValue || expiration.HasValue)
                {
                    var data = new AdditionalMessageData();
                    if (delay.HasValue)
                    {
                        data.SetDelay(delay.Value);
                    }
                    if (expiration.HasValue)
                    {
                        data.SetExpiration(expiration.Value);
                    }
                    messages.Add(new QueueMessage<SimpleMessage, IAdditionalMessageData>(message, data));
                }
                else
                {
                    messages.Add(new QueueMessage<SimpleMessage, IAdditionalMessageData>(message, null));
                }
            }
            return messages;
        }

        public ConsoleExecuteResult CreateQueue(string queueName, int type)
        {
            if (Enum.IsDefined(typeof(ConsumerQueueTypes), type))
            {
                CreateModuleIfNeeded(queueName, (ConsumerQueueTypes)type);
                return new ConsoleExecuteResult($"{queueName} has been created");
            }
            return new ConsoleExecuteResult($"Invalid queue type {type}. Valid values are 0=POCO,1=Linq Expression");
        }

        private void CreateModuleIfNeeded(string queueName, ConsumerQueueTypes type)
        {
            if (!_queues.ContainsKey(queueName))
            {
                switch (type)
                {
                    case ConsumerQueueTypes.Poco:
                        _queues.Add(queueName,
                           _queueContainer.Value.CreateProducer<SimpleMessage>(new QueueConnection(queueName,
                               ConfigurationManager.AppSettings["Connection"])));
                        break;
                    case ConsumerQueueTypes.Method:
                        _queues.Add(queueName,
                          _queueContainer.Value.CreateMethodProducer(new QueueConnection(queueName,
                              ConfigurationManager.AppSettings["Connection"])));
                        break;
                }
            }
        }
    }
}
