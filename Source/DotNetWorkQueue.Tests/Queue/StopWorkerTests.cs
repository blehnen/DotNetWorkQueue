using System;
using System.Collections.Generic;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class StopWorkerTests
    {
        [Fact]
        public void Stop_Workers_Null_Fails()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    test.Stop(null);
                });
        }

        [Fact]
        public void Stop_Workers()
        {
            var test = Create();
            var workers = new List<IWorker>
            {
                Substitute.For<IWorker>(),
                Substitute.For<IWorker>(),
                Substitute.For<IWorker>()
            };
            test.Stop(workers);
            foreach (var worker in workers)
            {
                worker.Received(1).Dispose();
            }
        }

        [Fact]
        public void Cancel_Set()
        {
            var cancellation = new CancellationTokenSource();
            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.StopTokenSource.Returns(cancellation);
            var test = Create(cancel);
            test.SetCancelTokenForStopping();
            Assert.True(cancellation.IsCancellationRequested);
        }

        private StopWorker Create(IQueueCancelWork cancelWork)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(cancelWork);
            return fixture.Create<StopWorker>();
        }

        private StopWorker Create()
        {
            var cancellation = new CancellationTokenSource();
            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.CancellationTokenSource.Returns(cancellation);
            cancel.StopTokenSource.Returns(cancellation);
            return Create(cancel);
        }
    }
}
