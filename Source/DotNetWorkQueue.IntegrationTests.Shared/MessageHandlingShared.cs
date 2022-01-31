using System;
using System.Collections.Concurrent;
using System.Threading;
using Xunit;

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
        public static void HandleFakeMessagesError<TMessage>(IncrementWrapper processedCount, ManualResetEventSlim waitForFinish, int messageCount, IReceivedMessage<TMessage> message)
         where TMessage : class
        {
            var currentCount = processedCount.GetErrorCount(message.MessageId.Id.Value.ToString());
            if (currentCount == 0)
            {
                Assert.False(message.PreviousErrors.ContainsKey(typeof(IndexOutOfRangeException).ToString()));
            }
            else
            {
                var messageCountFromMessage = Convert.ToInt64(message.PreviousErrors[typeof(IndexOutOfRangeException).ToString()]);

                //our count and the message count should match
                Assert.Equal(currentCount, messageCountFromMessage);
            }

            Interlocked.Increment(ref processedCount.ProcessedCount);
            processedCount.AddUpdateErrorCount(message.MessageId.Id.Value.ToString(), currentCount + 1);
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
