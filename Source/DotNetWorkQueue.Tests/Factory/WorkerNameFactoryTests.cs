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

using System.Collections.Concurrent;
using System.Threading;
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class WorkerNameFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            Assert.NotNull(factory.Create());
        }
        [Fact]
        public void Create_Multi_Threaded()
        {
            const int numThreads = 25;

            var names = new ConcurrentDictionary<string, string>();

            var factory = Create();
            var resetEvent = new ManualResetEvent(false);
            var toProcess = numThreads;
            for (var i = 0; i < numThreads; i++)
            {
                new Thread(delegate()
                {
                    var name = factory.Create();
                    names.TryAdd(name, name);
                    if (Interlocked.Decrement(ref toProcess) == 0)
                        resetEvent.Set();
                }).Start();
            }
            resetEvent.WaitOne();
            Assert.Equal(names.Count, numThreads);
        }
        private IWorkerNameFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerNameFactory>();
        }
    }
}
