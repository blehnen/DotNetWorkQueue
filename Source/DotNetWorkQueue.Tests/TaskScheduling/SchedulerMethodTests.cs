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
using System;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SchedulerMethodTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create(1))
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.Equal(test.IsDisposed, true);
            }
        }

        [Fact]
        public void Disposed_Configuration_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Configuration.SetReadOnly();
            });
            }
        }

        [Fact]
        public void Disposed_Start_Exception()
        {
            using (var test = Create(1))
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Start();
            });
            }
        }

        [Fact]
        public void Call_Start_Only_Once_Exception()
        {
            using (var test = Create(1))
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
            delegate
            {
                test.Start();
            });
            }
        }

        private SchedulerMethod Create(int workerCount, IWorkGroup workgroup = null)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var cancelWork = fixture.Create<IQueueCancelWork>();
            var workerConfiguration = fixture.Create<IWorkerConfiguration>();
            workerConfiguration.WorkerCount.Returns(workerCount);

            fixture.Inject(cancelWork);
            fixture.Inject(workerConfiguration);

            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);

            IConsumerQueueAsync queue = fixture.Create<ConsumerQueueAsync>();
            var fixture2 = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture2.Inject(queue);

            IConsumerQueueScheduler scheulder = fixture2.Create<Scheduler>();
            fixture2.Inject(scheulder);

            if (workgroup != null)
                fixture2.Inject(workgroup);

            var handler = fixture2.Create<SchedulerMessageHandler>();
            fixture2.Inject(handler);

            return fixture2.Create<SchedulerMethod>();
        }
    }
}
