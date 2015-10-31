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
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class HeartBeatThreadPoolFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            factory.Create();
        }
        [Fact]
        public void Create_Default_Multi_Threads()
        {
            var factory = Create();

            var numThreads = 20;
            var resetEvent = new ManualResetEvent(false);
            var toProcess = numThreads;
            for (var i = 0; i < numThreads; i++)
            {
                new Thread(delegate()
                {
                    factory.Create();
                    if (Interlocked.Decrement(ref toProcess) == 0)
                        resetEvent.Set();
                }).Start();
            }
            resetEvent.WaitOne();

        }
        public IHeartBeatThreadPoolFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var container = fixture.Create<IContainer>();
            fixture.Inject(container);
            container.GetInstance<IHeartBeatThreadPool>().Returns(fixture.Create<HeartBeatThreadPool>());
            return fixture.Create<HeartBeatThreadPoolFactory>();
        }
    }
}
