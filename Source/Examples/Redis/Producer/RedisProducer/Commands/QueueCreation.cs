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
using System.Text;
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.Redis.Basic;
namespace RedisProducer.Commands
{
    public class QueueCreation : IConsoleCommand, IDisposable
    {
        private readonly Lazy<QueueCreationContainer<RedisQueueInit>> _queueCreation;
        private readonly Dictionary<string, RedisQueueCreation> _queueCreators;

        public QueueCreation()
        {
            _queueCreation = new Lazy<QueueCreationContainer<RedisQueueInit>>(() => new QueueCreationContainer<RedisQueueInit>());
            _queueCreators = new Dictionary<string, RedisQueueCreation>();
        }

        public ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("QueueCreation", "Removes queues"));

        public ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine(ConsoleFormatting.FixedLength("RemoveQueue queueName", "Removes the queue from the transport"));
            return new ConsoleExecuteResult(help.ToString());
        }

        public ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "RemoveQueue":
                    return new ConsoleExecuteResult("RemoveQueue examplequeue");
            }
            return new ConsoleExecuteResult("Command not found");
        }

        public ConsoleExecuteResult RemoveQueue(string queueName)
        {
            CreateModuleIfNeeded(queueName);

            if (!_queueCreators[queueName].QueueExists) return new ConsoleExecuteResult("Queue does not exist");
            var result = _queueCreators[queueName].RemoveQueue();
            return !result.Success
                ? new ConsoleExecuteResult($"Failed to remove queue. Result is {result.Status}")
                : new ConsoleExecuteResult($"Removed queue; result is {result.Status}");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            foreach (var queue in _queueCreators.Values)
            {
                queue.Dispose();
            }
            _queueCreators.Clear();
            if (_queueCreation.IsValueCreated)
            {
                _queueCreation.Value.Dispose();
            }
        }

        private void CreateModuleIfNeeded(string queueName)
        {
            if (!_queueCreators.ContainsKey(queueName))
            {
                _queueCreators.Add(queueName,
                    _queueCreation.Value.GetQueueCreation<RedisQueueCreation>(new QueueConnection(queueName,
                        ConfigurationManager.AppSettings["Connection"])));
            }
        }
    }
}
