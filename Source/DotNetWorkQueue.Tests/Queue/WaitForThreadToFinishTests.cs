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
using System.Diagnostics;
using System.Threading;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
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
