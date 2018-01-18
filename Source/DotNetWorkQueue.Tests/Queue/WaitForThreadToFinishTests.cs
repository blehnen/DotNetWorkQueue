using System;
using System.Diagnostics;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WaitForThreadToFinishTests
    {
        [Fact]
        public void Wait()
        {
            var t = new Thread(RunMe);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.InRange(watch.ElapsedMilliseconds, 2950, 4250);
        }

        [Fact]
        public void Wait_Long()
        {
            var t = new Thread(RunMeLong);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.InRange(watch.ElapsedMilliseconds, 6950, 9000);
        }

        [Fact]
        public void Wait_With_Timeout()
        {
            var t = new Thread(RunMe);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t, TimeSpan.FromMilliseconds(1000));
            watch.Stop();
            Assert.InRange(watch.ElapsedMilliseconds, 950, 3000);
        }

        private WaitForThreadToFinish Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForThreadToFinish>();
        }

        private void RunMe()
        {
            Thread.Sleep(3000);
        }

        private void RunMeLong()
        {
            Thread.Sleep(7000);
        }
    }
}
