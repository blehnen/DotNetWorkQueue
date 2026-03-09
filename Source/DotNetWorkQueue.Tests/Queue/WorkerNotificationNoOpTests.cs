using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WorkerNotificationNoOpTests
    {
        [TestMethod]
        public void HeaderNames_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.HeaderNames);
        }

        [TestMethod]
        public void HeartBeat_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.HeartBeat);
            ob.HeartBeat = new WorkerHeartBeatNotificationNoOp();
            Assert.IsNull(ob.HeartBeat);
        }

        [TestMethod]
        public void Log_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.Log);
        }

        [TestMethod]
        public void Metrics_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.Metrics);
        }

        [TestMethod]
        public void Tracer_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.Tracer);
        }

        [TestMethod]
        public void TransportSupportsRollback_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsFalse(ob.TransportSupportsRollback);
        }

        [TestMethod]
        public void WorkerStopping_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.IsNull(ob.WorkerStopping);
            ob.WorkerStopping = new QueueCancelWork();
            Assert.IsNull(ob.WorkerStopping);
        }
    }
}
