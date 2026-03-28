using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
        public void Wait_Null_Task_Returns_True()
        {
            var test = Create();
            Assert.IsTrue(test.Wait(null));
        }

        [TestMethod]
        public void Wait_Completed_Task_Returns_True()
        {
            var t = Task.CompletedTask;
            var test = Create();
            Assert.IsTrue(test.Wait(t));
        }

        [TestMethod]
        public void Wait()
        {
            var t = Task.Factory.StartNew(() => Task.Delay(3000).Wait(), TaskCreationOptions.LongRunning);
            var watch = new Stopwatch();
            watch.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.IsTrue(watch.ElapsedMilliseconds >= 2950 && watch.ElapsedMilliseconds <= 4250,
                $"Expected elapsed time between 2950 and 4250 ms, but was {watch.ElapsedMilliseconds} ms");
        }

        [TestMethod]
        public void Wait_Long()
        {
            var t = Task.Factory.StartNew(() => Task.Delay(7000).Wait(), TaskCreationOptions.LongRunning);
            var watch = new Stopwatch();
            watch.Start();
            var test = Create();
            test.Wait(t);
            watch.Stop();
            Assert.IsTrue(watch.ElapsedMilliseconds >= 6950 && watch.ElapsedMilliseconds <= 9000,
                $"Expected elapsed time between 6950 and 9000 ms, but was {watch.ElapsedMilliseconds} ms");
        }

        [TestMethod]
        public void Wait_With_Timeout()
        {
            var t = Task.Factory.StartNew(() => Task.Delay(3000).Wait(), TaskCreationOptions.LongRunning);
            var watch = new Stopwatch();
            watch.Start();
            var test = Create();
            test.Wait(t, TimeSpan.FromMilliseconds(1000));
            watch.Stop();
            Assert.IsTrue(watch.ElapsedMilliseconds >= 950 && watch.ElapsedMilliseconds <= 3000,
                $"Expected elapsed time between 950 and 3000 ms, but was {watch.ElapsedMilliseconds} ms");
        }

        private WaitForThreadToFinish Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForThreadToFinish>();
        }
    }
}
