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
using DotNetWorkQueue.Transport.LiteDb.Basic;

namespace LiteDbProducer.Commands
{
    public class QueueCreation : IConsoleCommand, IDisposable
    {
        private readonly Lazy<QueueCreationContainer<LiteDbMessageQueueInit>> _queueCreation;
        private readonly Dictionary<string, LiteDbMessageQueueCreation> _queueCreators;

        public QueueCreation()
        {
            _queueCreation = new Lazy<QueueCreationContainer<LiteDbMessageQueueInit>>(() => new QueueCreationContainer<LiteDbMessageQueueInit>());
            _queueCreators = new Dictionary<string, LiteDbMessageQueueCreation>();
        }

        public ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("QueueCreation", "Creates and removes queues"));

        public ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine("");
            help.AppendLine(ConsoleFormatting.FixedLength("CreateQueue queueName", "Creates the queue in the transport"));
            help.AppendLine(ConsoleFormatting.FixedLength("RemoveQueue queueName", "Removes the queue from the transport"));

            help.AppendLine("Queue options (set before CreateQueue)");
            help.AppendLine("");

            help.AppendLine(ConsoleFormatting.FixedLength("SetQueueType queueName", "Type of the queue; needed for Rpc. 1=NotRpc,2=sendRpc,3=receiveRpc"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetDelayedProcessing queueName", "Enables/Disables delayed processing"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetHeartBeat queueName", "Enables/Disables heart beat support"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetMessageExpiration queueName", "Enables/Disables message expiration"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetStatusTable queueName", "Enables/Disables a separate status tracking table for external code to query"));
            help.AppendLine("");
            return new ConsoleExecuteResult(help.ToString());
        }

        public virtual ConsoleExecuteResult Example(string command)
        {
            switch (command)
            {
                case "CreateQueue":
                    return new ConsoleExecuteResult("CreateQueue examplequeue");
                case "RemoveQueue":
                    return new ConsoleExecuteResult("RemoveQueue examplequeue");

                case "SetQueueType":
                    return new ConsoleExecuteResult("SetQueueType examplequeue 0");
                case "SetDelayedProcessing":
                    return new ConsoleExecuteResult("SetDelayedProcessing examplequeue true");
                case "SetHeartBeat":
                    return new ConsoleExecuteResult("SetHeartBeat examplequeue true");
                case "SetMessageExpiration":
                    return new ConsoleExecuteResult("RemoveQueue examplequeue true");
                case "SetPriority":
                    return new ConsoleExecuteResult("SetPriority examplequeue true");
                case "SetStatus":
                    return new ConsoleExecuteResult("SetStatus examplequeue true");
                case "SetStatusTable":
                    return new ConsoleExecuteResult("SetStatusTable examplequeue true");
            }
            return new ConsoleExecuteResult("Command not found");
        }

        public ConsoleExecuteResult SetDelayedProcessing(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableDelayedProcessing = value;
            return new ConsoleExecuteResult($"DelayedProcessing set to {value}");
        }


        public ConsoleExecuteResult SetMessageExpiration(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableMessageExpiration = value;
            return new ConsoleExecuteResult($"MessageExpiration set to {value}");
        }


        public ConsoleExecuteResult SetStatusTable(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableStatusTable = value;
            return new ConsoleExecuteResult($"StatusTable set to {value}");
        }


        public ConsoleExecuteResult CreateQueue(string queueName)
        {
            CreateModuleIfNeeded(queueName);

            //create the queue if it doesn't exist
            if (_queueCreators[queueName].QueueExists) return new ConsoleExecuteResult("Queue already exists");

            //create the queue
            var valid = _queueCreators[queueName].Options.ValidConfiguration();
            if (!valid.Valid) return new ConsoleExecuteResult($"Configuration is invalid. {valid.ErrorMessage}");
            var result = _queueCreators[queueName].CreateQueue();
            return !result.Success
                ? new ConsoleExecuteResult($"Failed to create queue. Error message is {result.ErrorMessage}")
                : new ConsoleExecuteResult($"Created queue; result is {result.Status}");
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
                    _queueCreation.Value.GetQueueCreation<LiteDbMessageQueueCreation>(new QueueConnection(queueName,
                        ConfigurationManager.AppSettings["Connection"])));
            }
        }
    }
}
