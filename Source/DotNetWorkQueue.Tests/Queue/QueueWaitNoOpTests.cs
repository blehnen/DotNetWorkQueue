using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueWaitNoOpTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            test.Wait();
        }

        [Fact]
        public void Create_Default_Wait()
        {
            var test = Create();
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 0, 100); //integration server is overloaded; 50 sometimes fails for the max...
        }

        [Fact]
        public void Create_Default_Wait_Reset_Wait()
        {
            var test = Create();
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 0, 100);//integration server is overloaded; 50 sometimes fails for the max...

            test.Reset();

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 0, 100);//integration server is overloaded; 50 sometimes fails for the max...
        }

        private IQueueWait Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueWaitNoOp>();
        }
    }
}
