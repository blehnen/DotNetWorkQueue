// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerCollectionTests
    {
        [Fact]
        public void Start_Multiple_Times_Fails()
        {
            using (var test = Create(2))
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
               delegate
               {
                   test.Start();
               });
            }
        }

        [Fact]
        public void Stop_Multiple_Times_Ok()
        {
            using (var test = Create(2))
            {
                test.Start();
                test.Stop();
                test.Stop();
            }
        }

        [Fact]
        public void Stop_Without_Start_ok()
        {
            using (var test = Create(2))
            {
                test.Stop();
            }
        }

        private WorkerCollection Create(int workerCount)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<IWorkerConfiguration>();
            configuration.WorkerCount.Returns(workerCount);
            fixture.Inject(configuration);

            return fixture.Create<WorkerCollection>();
        }
    }
}
