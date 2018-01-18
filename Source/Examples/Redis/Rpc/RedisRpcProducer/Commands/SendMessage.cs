// ---------------------------------------------------------------------
// Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Interceptors;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Redis;
using DotNetWorkQueue.Transport.Redis.Basic;
using ExampleMessage;

namespace RedisRpcProducer.Commands
{
    public class SendMessage : SharedCommands
    {
        private readonly Lazy<QueueContainer<RedisQueueInit>> _queueContainer;
        private readonly Dictionary<string, IRpcBaseQueue> _queues;

        public SendMessage()
        {
            _queueContainer = new Lazy<QueueContainer<RedisQueueInit>>(CreateContainer);
            _queues = new Dictionary<string, IRpcBaseQueue>();
        }

        public override ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("SendMessage", "Sends messages to a queue"));

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
                    return new ConsoleExecuteResult("Send examplequeue 10 100 true null null");
                case "SendAsync":
                    return new ConsoleExecuteResult("SendAsync examplequeue 10 100 true null null");
            }
            return base.Example(command);
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

        protected override ConsoleExecuteResult ValidateQueue(string queueName)
        {
            if (!_queues.ContainsKey(queueName)) return new ConsoleExecuteResult($"{queueName} was not found. Call CreateQueue to create the queue first");
            return null;
        }

        public ConsoleExecuteResult Send(string queueName,
           int itemCount,
           TimeSpan? rpcTimeout = null,
           int runtime = 100,
           TimeSpan? delay = null,
           TimeSpan? expiration = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            if (!_queues[queueName].Started)
            {
                _queues[queueName].Start();
            }

            var timeout = TimeSpan.FromSeconds(5);
            if (rpcTimeout.HasValue)
            {
                timeout = rpcTimeout.Value;
            }

            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration);
            var returnMessage = new StringBuilder();
            foreach (var message in messages)
            {
                try
                {
                    var response = ((IRpcQueue<SimpleResponse, SimpleMessage>)_queues[queueName]).Send(message.Message, timeout, message.MessageData);
                    if (response.Body == null)
                    {
                        //RPC call failed
                        //do we have an exception?
                        var error =
                            response.GetHeader(
                               _queues[queueName].Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
                        if (error != null)
                        {
                            returnMessage.AppendLine(
                                "The consumer encountered an error trying to process our request");
                            returnMessage.AppendLine(error.ToString());
                        }
                        else
                        {
                            returnMessage.AppendLine(
                                "A null reply was received, but no error information was found. Examine the log to see if additional information can be found");
                        }
                    }
                    else
                    {
                        returnMessage.AppendLine(response.Body.Message);
                    }
                }
                catch (TimeoutException)
                {
                    returnMessage.AppendLine("The send request has timed out while waiting for a response");
                }
            }

            return new ConsoleExecuteResult(returnMessage.ToString());
        }

        public async Task<ConsoleExecuteResult> SendAsync(string queueName,
           int itemCount,
           TimeSpan? rpcTimeout = null,
           int runtime = 100,
           TimeSpan? delay = null,
           TimeSpan? expiration = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;

            if (!_queues[queueName].Started)
            {
                _queues[queueName].Start();
            }

            var timeout = TimeSpan.FromSeconds(5);
            if (rpcTimeout.HasValue)
            {
                timeout = rpcTimeout.Value;
            }

            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration);
            var returnMessage = new StringBuilder();
            foreach (var message in messages)
            {
                try
                {
                    var response = await ((IRpcQueue<SimpleResponse, SimpleMessage>)_queues[queueName]).SendAsync(message.Message, timeout, message.MessageData).ConfigureAwait(false);
                    if (response.Body == null)
                    {
                        //RPC call failed
                        //do we have an exception?
                        var error =
                            response.GetHeader(
                               _queues[queueName].Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
                        if (error != null)
                        {
                            returnMessage.AppendLine(
                                "The consumer encountered an error trying to process our request");
                            returnMessage.AppendLine(error.ToString());
                        }
                        else
                        {
                            returnMessage.AppendLine(
                                "A null reply was received, but no error information was found. Examine the log to see if additional information can be found");
                        }
                    }
                    else
                    {
                        returnMessage.AppendLine(response.Body.Message);
                    }
                }
                catch (TimeoutException)
                {
                    returnMessage.AppendLine("The send request has timed out while waiting for a response");
                }
            }

            return new ConsoleExecuteResult(returnMessage.ToString());
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
            if (_queues.ContainsKey(queueName)) return;
            var connection = ConfigurationManager.AppSettings["Connection"];

            switch (type)
            {
                case ConsumerQueueTypes.Poco:
                    _queues.Add(queueName,
                        _queueContainer.Value.CreateRpc<SimpleResponse, SimpleMessage, RedisQueueRpcConnection>(
                            new RedisQueueRpcConnection(connection, queueName)));
                    break;
                case ConsumerQueueTypes.Method:
                    _queues.Add(queueName,
                        _queueContainer.Value.CreateMethodRpc(
                            new RedisQueueRpcConnection(connection, queueName)));
                    break;
            }
            QueueStatus?.AddStatusProvider(QueueStatusContainer.Value.CreateStatusProvider<RedisQueueInit>(queueName, connection));
        }
    }
}
