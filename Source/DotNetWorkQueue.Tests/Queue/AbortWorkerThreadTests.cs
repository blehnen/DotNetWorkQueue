// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.Threading;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class AbortWorkerThreadTests
    {
        [Fact]
        public void If_Abort_Disabled_Return_Failure()
        {
            var test = Create(false);
            Assert.Equal(test.Abort(null), false);
        }
        [Fact]
        public void Abort_Allows_Null_Thread()
        {
            var test = Create(true);
            Assert.Equal(test.Abort(null), true);
        }

        [Fact]
        public void Abort_Allows_Stopped_Thread()
        {
            var test = Create(true);
            var t = new Thread(DoSomeWork);
            Assert.Equal(test.Abort(t), true);
        }

        [Fact]
        public void Abort_Thread()
        {
            var test = Create(true);

            var t = new Thread(DoSomeWork);
            t.Start();

            Assert.Equal(test.Abort(t), true);
            Thread.Sleep(500);
            Assert.Equal(t.IsAlive, false);
        }

        private IAbortWorkerThread Create(bool enableAbort)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IWorkerConfiguration>();
            configuration.AbortWorkerThreadsWhenStopping.Returns(enableAbort);
            fixture.Inject(configuration);
            return fixture.Create<AbortWorkerThread>();
        }

        private void DoSomeWork()
        {
            Thread.Sleep(100000);
        }
    }
}
