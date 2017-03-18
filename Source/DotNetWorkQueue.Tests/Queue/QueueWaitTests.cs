// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Xunit;
// ReSharper disable MethodSupportsCancellation
namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueWaitTests
    {
        [Fact]
        public void Create_Default()
        {
            var times = new List<TimeSpan> {TimeSpan.FromMilliseconds(100)};
            var test = new QueueWait(times, GetCancel());
            test.Wait();
        }

        [Fact]
        public void Create_Default_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 1500);
        }

        [Fact]
        public void Create_Default_1000_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 2000);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 2000);
        }

        [Fact]
        public void Create_Default_1000_2000_Reset_1000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(3000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 2000);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 1950, 2900);

            test.Reset();

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 2900);
        }

        [Fact]
        public void Create_Default_1000_2000_3000_Wait()
        {
            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(3000) };
            var test = new QueueWait(times, GetCancel());
            var watch = new Stopwatch();
            watch.Start();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 950, 2000);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 1950, 2900);

            watch.Restart();
            test.Wait();
            watch.Stop();

            Assert.InRange(watch.ElapsedMilliseconds, 2950, 3900);
        }

        [Fact]
        public void Create_Default_Cancel()
        {
            var cancelSource = new CancellationTokenSource();

            var times = new List<TimeSpan> { TimeSpan.FromMilliseconds(10000) };
            var test = new QueueWait(times, GetCancel(cancelSource));

            var watch = new Stopwatch();
            watch.Start();
            Task.Factory.StartNew(() => test.Wait()).ContinueWith(task => watch.Stop());
            cancelSource.Cancel();
            Assert.InRange(watch.ElapsedMilliseconds, 0, 5000);
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
