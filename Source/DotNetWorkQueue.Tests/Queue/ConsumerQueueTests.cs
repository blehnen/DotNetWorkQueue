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
    public class ConsumerQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [Theory, AutoData]
        public void Disposed_Instance_Get_Configuration_Exception(int value)
        {
            var test = CreateQueue();
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
            var test = CreateQueue();
            test.Dispose();
            Action<IReceivedMessage<FakeMessage>, IWorkerNotification> action = (message, worker) => { };
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Start(action);
                });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = CreateQueue())
            {
                Action<IReceivedMessage<FakeMessage>, IWorkerNotification> action = (message, worker) => { };
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
            using (var test = CreateQueue())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.Start<FakeMessage>(null);
                    });
            }
        }

        private ConsumerQueue CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var cancelWork = fixture.Create<IQueueCancelWork>();
            fixture.Inject(cancelWork);
            cancelWork.CancellationTokenSource.Returns(new CancellationTokenSource());
            cancelWork.StopTokenSource.Returns(new CancellationTokenSource());

            var stopWorker = fixture.Create<StopWorker>();
            fixture.Inject(stopWorker);
            return fixture.Create<ConsumerQueue>();
        }
    }
}
