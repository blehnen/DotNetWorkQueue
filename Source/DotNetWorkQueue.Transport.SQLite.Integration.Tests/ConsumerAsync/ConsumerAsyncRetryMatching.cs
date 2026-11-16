using System;
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IntegrationTests.Shared;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsumerKind = DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncRetryMatching.ConsumerKind;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests.ConsumerAsync
{
    /// <summary>
    /// Both async consumers must match retry rules on the exception the handler threw, as the sync
    /// consumer does - not on its inner exception, and not on a wrapper around it.
    /// </summary>
    /// <remarks>
    /// Runs on SQLite rather than the in-memory transport: Memory does not retry at all, so every row
    /// would report one attempt whichever rule matched.
    /// </remarks>
    [TestClass]
    [Retry(1)]
    public class ConsumerAsyncRetryMatching
    {
        [TestMethod]
        [DataRow(ConsumerKind.Scheduler, false, 3)]
        [DataRow(ConsumerKind.Scheduler, true, 1)]
        [DataRow(ConsumerKind.DirectAsync, false, 3)]
        [DataRow(ConsumerKind.DirectAsync, true, 1)]
        public void Run(ConsumerKind consumerKind, bool wrapTheRetryableException, int expectedAttemptsPerMessage)
        {
            // The retry rule is for IndexOutOfRangeException. Wrapped, the handler's exception is an
            // InvalidDataException that merely carries one, so it must not be retried.
            Func<Exception> handlerException = wrapTheRetryableException
                ? () => new InvalidDataException("handler failed", new IndexOutOfRangeException("cause"))
                : () => new IndexOutOfRangeException("handler failed");

            using (var connectionInfo = new IntegrationConnectionInfo(true))
            {
                var queueName = GenerateQueueName.Create();
                new DotNetWorkQueue.IntegrationTests.Shared.ConsumerAsync.Implementation.ConsumerAsyncRetryMatching()
                    .Run<SqLiteMessageQueueInit, FakeMessage, SqLiteMessageQueueCreation>(
                        new QueueConnection(queueName, connectionInfo.ConnectionString),
                        5, 2, consumerKind, handlerException, expectedAttemptsPerMessage,
                        x => Helpers.SetOptions(x, false, true, false, false, true, true, false),
                        Helpers.GenerateData, Helpers.Verify);
            }
        }
    }
}
