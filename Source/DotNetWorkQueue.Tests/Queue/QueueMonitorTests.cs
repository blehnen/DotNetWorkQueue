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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueMonitorTests
    {
        [Fact]
        public void Create_Default()
        {
            using (var test = Create())
            {
                test.Start();
                test.Stop();
            }
        }

        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Equal(test.IsDisposed, true);
            }
        }

        [Fact]
        public void Start_Called_Only_Once_Exception()
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
        public void Start_Called_After_Dispose_Exception()
        {
            using (var test = Create())
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
        public void Stop_Called_After_Dispose_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Stop();
            });
            }
        }

        private IQueueMonitor Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var heartBeat = fixture.Create<IHeartBeatConfiguration>();
            heartBeat.Enabled.Returns(true);
            var message = fixture.Create<IMessageExpirationConfiguration>();
            message.Enabled.Returns(true);

            fixture.Inject(heartBeat);
            fixture.Inject(message);

            var transport = fixture.Create<TransportConfigurationReceive>();
            transport.HeartBeatSupported = true;
            transport.MessageExpirationSupported = true;
            fixture.Inject(transport);
            return fixture.Create<QueueMonitor>();
        }
    }
}
