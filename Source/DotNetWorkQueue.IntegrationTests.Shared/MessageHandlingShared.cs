// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Threading;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class MessageHandlingShared
    {
        public static void HandleFakeMessageNoOp()
        {
            
        }

        public static void HandleFakeMessagesThreadAbort(int waitTime)
        {
            Thread.Sleep(waitTime);
            throw new OperationCanceledException();
        }

        public static void HandleFakeMessages<TMessage>(IReceivedMessage<TMessage> message, 
            int runTime, IncrementWrapper processedCount, int messageCount, ManualResetEventSlim waitForFinish)
            where TMessage : class
        {
            if (runTime > 0)
                Thread.Sleep(runTime * 1000);

            Interlocked.Increment(ref processedCount.ProcessedCount);
            var body = message?.Body as FakeMessage;
            if (body != null)
            {
                var result = processedCount.AddId(body.Id);
                if (!result)
                {
                    waitForFinish.Set();
                }
            }
            if (Interlocked.Read(ref processedCount.ProcessedCount) == messageCount)
            {
                waitForFinish.Set();
            }
        }
        public static void HandleFakeMessagesError(IncrementWrapper processedCount, ManualResetEventSlim waitForFinish, int messageCount)
        {
            Interlocked.Increment(ref processedCount.ProcessedCount);
            if (Interlocked.Read(ref processedCount.ProcessedCount) == messageCount * 3)
            {
                waitForFinish.Set();
            }
            // ReSharper disable once UnthrowableException
            throw new IndexOutOfRangeException("The index is out of range");
        }
        public static void HandleFakeMessagesRollback<TMessage>(IReceivedMessage<TMessage> message,
            int runTime,
            IncrementWrapper processedCount,
            long messageCount,
            ManualResetEventSlim waitForFinish,
            ConcurrentDictionary<string, int> haveIProcessedYouBefore)
                where TMessage : class
        {
            var key = message.CorrelationId.Id.Value.ToString();
            if (haveIProcessedYouBefore.ContainsKey(key))
            {
                if (runTime > 0)
                    Thread.Sleep(runTime * 1000);

                Interlocked.Increment(ref processedCount.ProcessedCount);
                haveIProcessedYouBefore[key] = haveIProcessedYouBefore[key] + 1;
                if (Interlocked.Read(ref processedCount.ProcessedCount) == messageCount)
                {
                    waitForFinish.Set();
                }
                return;
            }
            haveIProcessedYouBefore.TryAdd(key, 0);
            throw new OperationCanceledException("I don't feel like processing this message");
        }
    }
}
