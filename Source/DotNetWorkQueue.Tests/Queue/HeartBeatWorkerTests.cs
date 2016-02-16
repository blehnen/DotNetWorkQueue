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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class HeartBeatWorkerTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Call_Stop_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Stop();
                test.Stop();
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [Fact]
        public void Disposed_Instance_Start_Exception()
        {
            var test = Create();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Start();
                });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = Create())
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
        public void Test_Send()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            using (var test = Create(TimeSpan.FromSeconds(5), 2, context, sendHeartBeat, true))
            {
                test.Start();
                Thread.Sleep(6000);
            }
            sendHeartBeat.Received(2).Send(context);
        }

        [Fact]
        public void Test_Send_Return_Value()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();

            sendHeartBeat.Send(context)
                .Returns(new HeartBeatStatus(Substitute.For<IMessageId>(), DateTime.UtcNow));

            using (var test = Create(TimeSpan.FromSeconds(5), 2, context, sendHeartBeat, true))
            {
                test.Start();
                Thread.Sleep(6000);
            }
            sendHeartBeat.Received(2).Send(context);
        }

        [Fact]
        public void Test_Send_Exception()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();

            sendHeartBeat.Send(context).Throws(new ArgumentOutOfRangeException());

            using (var test = Create(TimeSpan.FromSeconds(5), 2, context, sendHeartBeat, true))
            {
                test.Start();
                Thread.Sleep(6000);
            }
        }

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, 1, fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>(), false);
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, int interval, IMessageContext context, ISendHeartBeat sendHeartBeat, bool useThreadPool)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);
            var threadPoolConfiguration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            threadPoolConfiguration.ThreadsMax.Returns(1);
            threadPoolConfiguration.ThreadsMin.Returns(1);
            fixture.Inject(threadPoolConfiguration);
            IHeartBeatConfiguration configuration = fixture.Create<HeartBeatConfiguration>();
            configuration.Time = checkSpan;
            configuration.Interval = interval;
            fixture.Inject(configuration);
            IHeartBeatThreadPool threadpool;
            if (!useThreadPool)
            {
                threadpool = fixture.Create<IHeartBeatThreadPool>();
            }
            else
            {
                threadpool = fixture.Create<HeartBeatThreadPool>();
                threadpool.Start();
            }
            fixture.Inject(threadpool);
            return fixture.Create<HeartBeatWorker>();
        }
    }
}
