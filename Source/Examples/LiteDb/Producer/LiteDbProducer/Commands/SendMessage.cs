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
using DotNetWorkQueue.Transport.LiteDb;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using ExampleMessage;

namespace LiteDbProducer.Commands
{
    public class SendMessage : SharedSendCommands
    {
        private readonly Lazy<QueueContainer<LiteDbMessageQueueInit>> _queueContainer;
        private readonly Dictionary<string, IProducerBaseQueue> _queues;

        private readonly object _asyncStringBuilderLock = new object();

        public SendMessage()
        {
            _queueContainer = new Lazy<QueueContainer<LiteDbMessageQueueInit>>(CreateContainer);
            _queues = new Dictionary<string, IProducerBaseQueue>();
        }

        protected override Dictionary<string, IProducerBaseQueue> Queues => _queues;

        public override ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("SendMessage", "Sends messages to a queue"));

        private QueueContainer<LiteDbMessageQueueInit> CreateContainer()
        {
            return new QueueContainer<LiteDbMessageQueueInit>(RegisterService);
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

        public override ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.Append(base.Help().Message);
            help.AppendLine(ConsoleFormatting.FixedLength("Send queueName", "Sends messages"));
            help.AppendLine(ConsoleFormatting.FixedLength("SendAsync queueName", "Sends messages async"));
            return new ConsoleExecuteResult(help.ToString());
        }

        public override ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "Send":
                    return new ConsoleExecuteResult("Send examplequeue 10 100 true null null null");
                case "SendAsync":
                    return new ConsoleExecuteResult("SendAsync examplequeue 10 100 true null null null");
            }
            return base.Example(command);
        }

        protected override ConsoleExecuteResult ValidateQueue(string queueName)
        {
            if (!_queues.ContainsKey(queueName)) return new ConsoleExecuteResult($"{queueName} was not found. Call CreateQueue to create the queue first");
            return null;
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
