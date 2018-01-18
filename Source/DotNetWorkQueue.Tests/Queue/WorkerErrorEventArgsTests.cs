using System;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerErrorEventArgsTests
    {
        [Fact]
        public void Default()
        {
            var e = new Exception();
            var worker = Substitute.For<IWorkerBase>();
            var test = new WorkerErrorEventArgs(worker, e);
            Assert.Equal(worker, test.Worker);
            Assert.Equal(e, test.Error);
        }
    }
}
