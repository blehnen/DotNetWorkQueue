using System;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerErrorEventArgsTests
    {
        [TestMethod]
        public void Default()
        {
            var e = new Exception();
            var worker = Substitute.For<IWorkerBase>();
            var test = new WorkerErrorEventArgs(worker, e);
            Assert.AreEqual(worker, test.Worker);
            Assert.AreEqual(e, test.Error);
        }
    }
}
