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
using System.Threading;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class StopWorkerTests
    {
        [Fact]
        public void Stop_Workers_Null_Fails()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    test.Stop(null);
                });
        }

        [Fact]
        public void Stop_Workers()
        {
            var test = Create();
            var workers = new List<IWorker>
            {
                Substitute.For<IWorker>(),
                Substitute.For<IWorker>(),
                Substitute.For<IWorker>()
            };
            test.Stop(workers);
            foreach (var worker in workers)
            {
                worker.Received(1).Dispose();
            }
        }

        [Fact]
        public void Cancel_Set()
        {
            var cancellation = new CancellationTokenSource();
            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.StopTokenSource.Returns(cancellation);
            var test = Create(cancel);
            test.SetCancelTokenForStopping();
            Assert.True(cancellation.IsCancellationRequested);
        }

        private StopWorker Create(IQueueCancelWork cancelWork)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(cancelWork);
            return fixture.Create<StopWorker>();
        }

        private StopWorker Create()
        {
            var cancellation = new CancellationTokenSource();
            var cancel = Substitute.For<IQueueCancelWork>();
            cancel.CancellationTokenSource.Returns(cancellation);
            cancel.StopTokenSource.Returns(cancellation);
            return Create(cancel);
        }
    }
}
