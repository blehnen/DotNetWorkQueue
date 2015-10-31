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

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class ConsumerQueueAsyncTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue(1);
            Assert.Equal(test.IsDisposed, false);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }


        [Theory, AutoData]
        public void Disposed_Instance_Get_Configuration_Exception(int value)
        {
            var test = CreateQueue(1);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Configuration.Worker.WorkerCount = value;
            });
        }

        [Fact]
        public void Disposed_Instance_Start_Exception()
        {
            var test = CreateQueue(1);
            test.Dispose();
            Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task> action = (message, worker) => null;
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Start(action);
            });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task> action = (message, worker) => null;
                test.Start(action);
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start(action);
                    });
            }
        }

        public void Calling_Worker_Count_Greater_Than_One_Exception()
        {
            using (var test = CreateQueue(2))
            {
                Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task> action = (message, worker) => null;
                test.Start(action);
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start(action);
                    });
            }
        }

        [Fact]
        public void Calling_Start_Null_Action_Exception()
        {
            using (var test = CreateQueue(1))
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.Start<FakeMessage>(null);
                    });
            }
        }

        private ConsumerQueueAsync CreateQueue(int workerCount)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var cancelWork = fixture.Create<IQueueCancelWork>();

            var workerConfiguration = fixture.Create<IWorkerConfiguration>();
            workerConfiguration.WorkerCount.Returns(workerCount);

            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            fixture.Inject(workerConfiguration);
            fixture.Inject(cancelWork);

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);

            return fixture.Create<ConsumerQueueAsync>();
        }
    }
}
