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
using ConsoleShared;
using DotNetWorkQueue;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Schema;

namespace SQLiteProducer.Commands
{
    public class QueueCreation : IConsoleCommand, IDisposable
    {
        private readonly Lazy<QueueCreationContainer<SqLiteMessageQueueInit>> _queueCreation;
        private readonly Dictionary<string, SqLiteMessageQueueCreation> _queueCreators;

        public QueueCreation()
        {
            _queueCreation = new Lazy<QueueCreationContainer<SqLiteMessageQueueInit>>(() => new QueueCreationContainer<SqLiteMessageQueueInit>());
            _queueCreators = new Dictionary<string, SqLiteMessageQueueCreation>();
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
            help.AppendLine(ConsoleFormatting.FixedLength("SetPriority queueName", "Enables/Disables message priority"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetStatus queueName", "Enables/Disables using a status column to flag pending/working items"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetStatusTable queueName", "Enables/Disables a separate status tracking table for external code to query"));
            help.AppendLine("");
            help.AppendLine(ConsoleFormatting.FixedLength("AddColumn queueName", "Adds a new user column to the status table"));
            help.AppendLine(ConsoleFormatting.FixedLength("AddColumnWithLength queueName", "Adds a new user column to the status table"));
            help.AppendLine(ConsoleFormatting.FixedLength("AddColumnWithPrecision queueName", "Adds a new user column to the status table"));
            help.AppendLine(ConsoleFormatting.FixedLength("AddConstraint queueName", "Adds a new user constraint to the status table"));
            help.AppendLine(ConsoleFormatting.FixedLength("AddConstraintManyColumns queueName", "Adds a new user constraint to the status table"));
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

                case "AddColumn":
                    return new ConsoleExecuteResult("AddColumn examplequeue OrderID BigInt true");
                case "AddColumnWithLength":
                    return new ConsoleExecuteResult("AddColumnWithLength examplequeue address varchar 100 true");
                case "AddColumnWithPrecision":
                    return new ConsoleExecuteResult("AddColumnWithPrecision examplequeue number decimal 12 6 true");
                case "AddConstraint":
                    return new ConsoleExecuteResult("AddConstraint examplequeue order_ix index OrderID");
                case "AddConstraintManyColumns":
                    return new ConsoleExecuteResult("AddConstraintManyColumns examplequeue order_ix index OrderID1,OrderID2");
            }
            return new ConsoleExecuteResult("Command not found");
        }

        public ConsoleExecuteResult SetQueueType(string queueName, int queueType)
        {
            if (Enum.IsDefined(typeof(QueueTypes), queueType))
            {
                var type = (QueueTypes)queueType;
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.QueueType = type;
                return new ConsoleExecuteResult($"QueueType set to {type}");
            }
            return new ConsoleExecuteResult($"invalid value {queueType}");
        }
        public ConsoleExecuteResult SetDelayedProcessing(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableDelayedProcessing = value;
            return new ConsoleExecuteResult($"DelayedProcessing set to {value}");
        }

        public ConsoleExecuteResult SetHeartBeat(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableHeartBeat = value;
            return new ConsoleExecuteResult($"HeartBeat set to {value}");
        }

        public ConsoleExecuteResult SetMessageExpiration(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableMessageExpiration = value;
            return new ConsoleExecuteResult($"MessageExpiration set to {value}");
        }

        public ConsoleExecuteResult SetPriority(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnablePriority = value;
            return new ConsoleExecuteResult($"Priority set to {value}");
        }

        public ConsoleExecuteResult SetStatus(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableStatus = value;
            return new ConsoleExecuteResult($"Status set to {value}");
        }

        public ConsoleExecuteResult SetStatusTable(string queueName, bool value)
        {
            CreateModuleIfNeeded(queueName);
            _queueCreators[queueName].Options.EnableStatusTable = value;
            return new ConsoleExecuteResult($"StatusTable set to {value}");
        }

        public ConsoleExecuteResult AddColumn(string queueName, string name, string type, bool @null = true)
        {
            if (Enum.TryParse(type, true, out ColumnTypes columnType))
            {
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.AdditionalColumns.Add(new Column(name, columnType, @null, null));
                return new ConsoleExecuteResult($"Added column {name}");
            }
            throw new Exception($"Failed to parse {type}");
        }

        public ConsoleExecuteResult AddColumnWithLength(string queueName, string name, string type, int length, bool @null = true)
        {
            if (Enum.TryParse(type, true, out ColumnTypes columnType))
            {
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.AdditionalColumns.Add(new Column(name, columnType, length, @null, null));
                return new ConsoleExecuteResult($"Added column {name}");
            }
            throw new Exception($"Failed to parse {type}");
        }

        public ConsoleExecuteResult AddColumnWithPrecision(string queueName, string name, string type, byte precision, int scale, bool @null = true)
        {
            if (Enum.TryParse(type, true, out ColumnTypes columnType))
            {
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.AdditionalColumns.Add(new Column(name, columnType, precision, scale, @null,
                    null));
                return new ConsoleExecuteResult($"Added column {name}");
            }
            throw new Exception($"Failed to parse {type}");
        }

        public ConsoleExecuteResult AddConstraint(string queueName, string name, string type, string column)
        {
            if (Enum.TryParse(type, true, out ConstraintType constraintType))
            {
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.AdditionalConstraints.Add(new Constraint(name, constraintType, column));
                return new ConsoleExecuteResult($"Added constraint {name}");
            }
            throw new Exception($"Failed to parse {type}");
        }

        public ConsoleExecuteResult AddConstraintManyColumns(string queueName, string name, string type, params string[] columns)
        {
            if (Enum.TryParse(type, true, out ConstraintType constraintType))
            {
                CreateModuleIfNeeded(queueName);
                _queueCreators[queueName].Options.AdditionalConstraints.Add(new Constraint(name, constraintType, columns.ToList()));
                return new ConsoleExecuteResult($"Added constraint {name}");
            }
            throw new Exception($"Failed to parse {type}");
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
                    _queueCreation.Value.GetQueueCreation<SqLiteMessageQueueCreation>(new QueueConnection(queueName,
                        ConfigurationManager.AppSettings["Connection"])));
            }
        }
    }
}
