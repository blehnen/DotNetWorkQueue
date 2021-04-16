using DotNetWorkQueue.Queue;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerNotificationNoOpTests
    {
        [Fact]
        public void HeaderNames_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.HeaderNames);
        }

        [Fact]
        public void HeartBeat_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.HeartBeat);
            ob.HeartBeat = new WorkerHeartBeatNotificationNoOp();
            Assert.Null(ob.HeartBeat);
        }

        [Fact]
        public void Log_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.Log);
        }

        [Fact]
        public void Metrics_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.Metrics);
        }

        [Fact]
        public void Tracer_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.Tracer);
        }

        [Fact]
        public void TransportSupportsRollback_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.False(ob.TransportSupportsRollback);
        }

        [Fact]
        public void WorkerStopping_Test()
        {
            var ob = new WorkerNotificationNoOp();
            Assert.Null(ob.WorkerStopping);
            ob.WorkerStopping = new QueueCancelWork();
            Assert.Null(ob.WorkerStopping);
        }
    }
}
