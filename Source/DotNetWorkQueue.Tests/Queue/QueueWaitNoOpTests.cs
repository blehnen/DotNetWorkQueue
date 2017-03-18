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
using System.Diagnostics;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
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
