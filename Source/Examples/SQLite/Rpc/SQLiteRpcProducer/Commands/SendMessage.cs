// ---------------------------------------------------------------------
// Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared;
using DotNetWorkQueue.Transport.SQLite.Shared.Basic;
using ExampleMessage;

namespace SQLiteRpcProducer.Commands
{
    public class SendMessage : SharedCommands
    {
        private readonly Lazy<QueueContainer<SqLiteMessageQueueInit>> _queueContainer;
        private readonly Dictionary<string, IRpcBaseQueue> _queues;

        public SendMessage()
        {
            _queueContainer = new Lazy<QueueContainer<SqLiteMessageQueueInit>>(CreateContainer);
            _queues = new Dictionary<string, IRpcBaseQueue>();
        }

        public override ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("SendMessage", "Sends messages to a queue and receives a response"));

        private QueueContainer<SqLiteMessageQueueInit> CreateContainer()
        {
            return new QueueContainer<SqLiteMessageQueueInit>(RegisterService);
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
            help.AppendLine("-The following commands send messages-");
            help.AppendLine("Send <name> int:itemCount bool:batched nullable<timespan>:delay nullable<timespan>:expiration nullable<ushort>:priority");
            help.AppendLine("SendAsync <name> int:itemCount bool:batched nullable<timespan>:delay nullable<timespan>:expiration nullable<ushort>:priority");
            help.Append(base.Help().Message);
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

        public ConsoleExecuteResult Send(string queueReceive,
            string queueNameSend,
           int itemCount,
           TimeSpan? rpcTimeout = null,
           int runtime = 100,
           TimeSpan? delay = null,
           TimeSpan? expiration = null,
           ushort? priority = null)
        {
            var valid = ValidateQueue(queueReceive);
            if (valid != null) return valid;

            if (!_queues[queueReceive].Started)
            {
                _queues[queueReceive].Start();
            }

            TimeSpan timeout = TimeSpan.FromSeconds(5);
            if (rpcTimeout.HasValue)
            {
                timeout = rpcTimeout.Value;
            }

            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration, priority);
            var returnMessage = new StringBuilder();
            foreach (var message in messages)
            {
                try
                {
                    var response = ((IRpcQueue<SimpleResponse, SimpleMessage>)_queues[queueReceive]).Send(message.Message, timeout, message.MessageData);
                    if (response.Body == null)
                    {
                        //RPC call failed
                        //do we have an exception?
                        var error =
                            response.GetHeader(
                               _queues[queueReceive].Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
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

        public async Task<ConsoleExecuteResult> SendAsync(string queueReceive,
            string queueNameSend,
           int itemCount,
           TimeSpan? rpcTimeout = null,
           int runtime = 100,
           TimeSpan? delay = null,
           TimeSpan? expiration = null,
           ushort? priority = null)
        {
            var valid = ValidateQueue(queueReceive);
            if (valid != null) return valid;

            if (!_queues[queueReceive].Started)
            {
                _queues[queueReceive].Start();
            }

            TimeSpan timeout = TimeSpan.FromSeconds(5);
            if (rpcTimeout.HasValue)
            {
                timeout = rpcTimeout.Value;
            }

            var messages = GenerateMessages(CreateMessages(itemCount, runtime).ToList(), delay, expiration, priority);
            var returnMessage = new StringBuilder();
            foreach (var message in messages)
            {
                try
                {
                    var response = await ((IRpcQueue<SimpleResponse, SimpleMessage>)_queues[queueReceive]).SendAsync(message.Message, timeout, message.MessageData).ConfigureAwait(false);
                    if (response.Body == null)
                    {
                        //RPC call failed
                        //do we have an exception?
                        var error =
                            response.GetHeader(
                               _queues[queueReceive].Configuration.HeaderNames.StandardHeaders.RpcConsumerException);
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
            TimeSpan? expiration = null,
            ushort? priority = null
            )
        {
            var messages = new List<QueueMessage<SimpleMessage, IAdditionalMessageData>>(jobs.Count);
            foreach (var message in jobs)
            {
                if (delay.HasValue || expiration.HasValue || priority.HasValue)
                {
                    var data = new AdditionalMessageData();
                    if (priority.HasValue)
                    {
                        data.SetPriority(priority.Value);
                    }
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

        public ConsoleExecuteResult CreateQueue(string queueNameReceive, string queueNameSend, int type)
        {
            if (Enum.IsDefined(typeof(ConsumerQueueTypes), type))
            {
                CreateModuleIfNeeded(queueNameReceive, queueNameSend, (ConsumerQueueTypes)type);
                return new ConsoleExecuteResult($"{queueNameReceive} has been created");
            }
            return new ConsoleExecuteResult($"Invalid queue type {type}. Valid values are 0=POCO,1=Linq Expression");
        }

        private void CreateModuleIfNeeded(string queueNameReceive, string queueNameResponse, ConsumerQueueTypes type)
        {
            if (!_queues.ContainsKey(queueNameReceive))
            {
                var connection = ConfigurationManager.AppSettings["Connection"];
                switch (type)
                {
                    case ConsumerQueueTypes.Poco:
                        _queues.Add(queueNameReceive,
                          _queueContainer.Value.CreateRpc<SimpleResponse, SimpleMessage, SqLiteRpcConnection>(
                              new SqLiteRpcConnection(connection, queueNameReceive, connection, queueNameResponse, new DbDataSource())));
                        break;
                    case ConsumerQueueTypes.Method:
                        _queues.Add(queueNameReceive,
                          _queueContainer.Value.CreateMethodRpc(
                              new SqLiteRpcConnection(connection, queueNameReceive, connection, queueNameResponse, new DbDataSource())));
                        break;
                }

                QueueStatus?.AddStatusProvider(QueueStatusContainer.Value.CreateStatusProvider<SqLiteMessageQueueInit>(queueNameReceive, connection));
                QueueStatus?.AddStatusProvider(QueueStatusContainer.Value.CreateStatusProvider<SqLiteMessageQueueInit>(queueNameResponse, connection));
            }
        }
    }
}
