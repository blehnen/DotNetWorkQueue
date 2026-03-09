using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class QueueWaitNoOpTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = Create();
            test.Wait();
        }

        [TestMethod]
        public void Create_Default_Wait()
        {
            var test = Create();
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(0L, 100L, watch.ElapsedMilliseconds); //integration server is overloaded; 50 sometimes fails for the max...
        }

        [TestMethod]
        public void Create_Default_Wait_Reset_Wait()
        {
            var test = Create();
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(0L, 100L, watch.ElapsedMilliseconds);//integration server is overloaded; 50 sometimes fails for the max...

            test.Reset();

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(0L, 100L, watch.ElapsedMilliseconds);//integration server is overloaded; 50 sometimes fails for the max...
        }

        private IQueueWait Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueWaitNoOp>();
        }
    }
}
