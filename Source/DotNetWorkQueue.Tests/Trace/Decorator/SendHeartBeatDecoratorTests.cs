// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetWorkQueue.Trace.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Trace.Decorator
{
    /// <summary>
    /// The tracing decorator over the heartbeat.
    ///
    /// It read <see cref="IHeartBeatStatus.LastHeartBeatTime"/> without checking it had a status to
    /// read it from. A transport returns null when the context has no message id - Redis does - so
    /// tracing turned that documented no-op into a crash, and <c>HeartBeatWorker</c> answers a throwing
    /// beat by logging a heartbeat failure and cancelling the message's token. A message whose
    /// heartbeat stops beating is one the monitor will hand to a second worker.
    /// </summary>
    [TestClass]
    public class SendHeartBeatDecoratorTests
    {
        [TestMethod]
        public void Send_WithNoStatus_DoesNotThrow()
        {
            var harness = new Harness(status: null);

            Assert.IsNull(harness.Decorator.Send(harness.Context));
        }

        [TestMethod]
        public async Task SendAsync_WithNoStatus_DoesNotThrow()
        {
            var harness = new Harness(status: null);

            Assert.IsNull(await harness.Decorator.SendAsync(harness.Context));
        }

        [TestMethod]
        public void Send_ReturnsTheStatusItWasGiven()
        {
            var harness = new Harness(status: Status(DateTime.UtcNow));

            Assert.AreSame(harness.Status, harness.Decorator.Send(harness.Context));
            harness.Decorated.Received(1).Send(harness.Context);
        }

        [TestMethod]
        public async Task SendAsync_ReturnsTheStatusItWasGiven()
        {
            var harness = new Harness(status: Status(DateTime.UtcNow));

            Assert.AreSame(harness.Status, await harness.Decorator.SendAsync(harness.Context));
            await harness.Decorated.Received(1).SendAsync(harness.Context);
        }

        [TestMethod]
        public async Task SendAsync_WithAStatusCarryingNoTime_DoesNotThrow()
        {
            //a beat that found no record to update comes back with no time on it
            var harness = new Harness(status: Status(null));

            Assert.AreSame(harness.Status, await harness.Decorator.SendAsync(harness.Context));
        }

        private static IHeartBeatStatus Status(DateTime? time)
        {
            var status = Substitute.For<IHeartBeatStatus>();
            status.LastHeartBeatTime.Returns(time);
            return status;
        }

        private sealed class Harness
        {
            public SendHeartBeatDecorator Decorator { get; }
            public ISendHeartBeat Decorated { get; }
            public IMessageContext Context { get; }
            public IHeartBeatStatus Status { get; }

            public Harness(IHeartBeatStatus status)
            {
                Status = status;
                Context = Substitute.For<IMessageContext>();
                Decorated = Substitute.For<ISendHeartBeat>();
                Decorated.Send(Context).Returns(status);
                Decorated.SendAsync(Context).Returns(Task.FromResult(status));

                Decorator = new SendHeartBeatDecorator(Decorated,
                    new ActivitySource("DotNetWorkQueue.Tests.HeartBeat"),
                    Substitute.For<IStandardHeaders>());
            }
        }
    }
}
