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
using System.Diagnostics.CodeAnalysis;
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
                Assert.False(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
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
            Assert.True(test.IsDisposed);
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
        public void Test_Send_Exception()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();

            sendHeartBeat.Send(context).Throws(new ArgumentOutOfRangeException());

            using (var test = Create(TimeSpan.FromSeconds(5), "sec(*%2)", context, sendHeartBeat))
            {
                test.Start();
                Thread.Sleep(7000);
            }
        }

        [Theory]
        [InlineData(5),
         InlineData(59),
         InlineData(65),
         InlineData(600),
         InlineData(6000),
         InlineData(60000),
         InlineData(600000)]
        public void Test_SendDiff(int seconds)
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            using (var test = Create(TimeSpan.FromSeconds(seconds), "sec(*%2)", context, sendHeartBeat))
            {
                test.Start();
                Thread.Sleep(1100);
            }
        }

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, "sec(*%59)", fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>());
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, string updateTime, IMessageContext context, ISendHeartBeat sendHeartBeat)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);
            var threadPoolConfiguration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            threadPoolConfiguration.ThreadsMax.Returns(1);
            fixture.Inject(threadPoolConfiguration);
            IHeartBeatConfiguration configuration = fixture.Create<HeartBeatConfiguration>();
            configuration.Time = checkSpan;
            configuration.UpdateTime = updateTime;
            fixture.Inject(configuration);
            var threadpool = fixture.Create<IHeartBeatScheduler>();
            fixture.Inject(threadpool);
            return fixture.Create<HeartBeatWorker>();
        }
    }
}
