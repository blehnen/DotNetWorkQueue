using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable MethodSupportsCancellation
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class QueueWaitTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(100) };
            var test = new QueueWait(times, GetCancel());
            test.Wait();
        }

        [TestMethod]
        public void Create_Default_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 1500L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Create_Default_1000_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 2000L, watch.ElapsedMilliseconds);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 2000L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Create_Default_1000_2000_Reset_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(3000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 2000L, watch.ElapsedMilliseconds);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(1950L, 2900L, watch.ElapsedMilliseconds);

            test.Reset();

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 2900L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Create_Default_1000_2000_3000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(3000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(950L, 2000L, watch.ElapsedMilliseconds);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(1950L, 2900L, watch.ElapsedMilliseconds);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.IsInRange(2950L, 3900L, watch.ElapsedMilliseconds);
        }

        [TestMethod]
        public void Create_Default_Cancel()
        {
            var cancelSource = new CancellationTokenSource();

            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(10000) };
            var test = new QueueWait(times, GetCancel(cancelSource));

            var watch = new Stopwatch();
            watch.Start();
            Task.Factory.StartNew(() => test.Wait(), TaskCreationOptions.LongRunning).ContinueWith(task => watch.Stop());
            cancelSource.Cancel();
            Assert.IsInRange(0L, 5000L, watch.ElapsedMilliseconds);
        }

        private ICancelWork GetCancel()
        {
            var cancelSource = new CancellationTokenSource();
            var stopSource = new CancellationTokenSource();

            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.CancellationTokenSource.Returns(cancelSource);
            cancel.StopTokenSource.Returns(stopSource);
            return cancel;
        }

        private ICancelWork GetCancel(CancellationTokenSource cancelSource)
        {
            var stopSource = new CancellationTokenSource();

            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.CancellationTokenSource.Returns(cancelSource);
            cancel.StopTokenSource.Returns(stopSource);
            return cancel;
        }
    }
}
