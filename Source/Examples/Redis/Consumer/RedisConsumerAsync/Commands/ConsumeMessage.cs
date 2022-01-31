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
using System.Text;
using ConsoleShared;
using ConsoleSharedCommands.Commands;
using DotNetWorkQueue.Transport.Redis;
using DotNetWorkQueue.Transport.Redis.Basic;

namespace RedisConsumerAsync.Commands
{
    public class ConsumeMessage : ConsumeMessageAsync<RedisQueueInit>
    {
        public override ConsoleExecuteResult Info => new ConsoleExecuteResult(ConsoleFormatting.FixedLength("ConsumeMessage", "Processes messages in a queue"));

        public override ConsoleExecuteResult Help()
        {
            var help = new StringBuilder();
            help.AppendLine(base.Help().Message);
            help.AppendLine(ConsoleFormatting.FixedLength("SetRedisOptions queueName", "Sets redis transport specific options"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetTimeClientOptions queueName", "Options for obtaining the current time. 0=localmachine,1=redisServer,2=NTP"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetMessageIdOptions queueName", "Message ID generation. 0=redis,1=uuid"));
            help.AppendLine(ConsoleFormatting.FixedLength("SetSntpTimeConfiguration queueName", "Options for the NTP client"));
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
            }
            return base.Example(command);
        }

        public ConsoleExecuteResult SetRedisOptions(string queueName,
            int clearExpiredMessagesBatchLimit = 50,
            int moveDelayedMessagesBatchLimit = 50,
            int resetHeartBeatBatchLimit = 50,
            TimeSpan? delayedProcessingMonitorTime = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            Queues[queueName].Configuration.Options().ClearExpiredMessagesBatchLimit = clearExpiredMessagesBatchLimit;
            if (delayedProcessingMonitorTime.HasValue)
            {
                Queues[queueName].Configuration.Options().DelayedProcessingConfiguration.MonitorTime = delayedProcessingMonitorTime.Value;
            }
            Queues[queueName].Configuration.Options().MoveDelayedMessagesBatchLimit = moveDelayedMessagesBatchLimit;
            Queues[queueName].Configuration.Options().ResetHeartBeatBatchLimit = resetHeartBeatBatchLimit;
            return new ConsoleExecuteResult("options set");
        }

        public ConsoleExecuteResult SetSntpTimeConfiguration(string queueName,
            int port = 123,
            string server = "pool.ntp.org",
            TimeSpan? timeout = null)
        {
            var valid = ValidateQueue(queueName);
            if (valid != null) return valid;
            Queues[queueName].Configuration.Options().SntpTimeConfiguration.Port = port;
            Queues[queueName].Configuration.Options().SntpTimeConfiguration.Server = server;
            if (timeout.HasValue)
            {
                Queues[queueName].Configuration.Options().SntpTimeConfiguration.TimeOut = timeout.Value;
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
                Queues[queueName].Configuration.Options().TimeServer = type;
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
                Queues[queueName].Configuration.Options().MessageIdLocation = type;
                return new ConsoleExecuteResult($"Set time client to {type}");
            }
            return new ConsoleExecuteResult($"invalid value {value}");
        }
    }
}
