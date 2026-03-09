using System;
using System.Diagnostics;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class WaitForThreadToFinishTests
    {
        [TestMethod]
        public void Wait()
        {
            var t = new Thread(RunMe);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.IsInRange(2950L, 4250L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Wait_Long()
        {
            var t = new Thread(RunMeLong);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.IsInRange(6950L, 9000L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Wait_With_Timeout()
        {
            var t = new Thread(RunMe);
            var watch = new Stopwatch();
            watch.Start();
            t.Start();
            var test = Create();
            test.Wait(t, TimeSpan.FromMilliseconds(1000));
            watch.Stop();
            Assert.IsInRange(950L, 3000L, watch.ElapsedMilliseconds);
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
