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
using System.Threading.Tasks;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class MessageHandlerTests
    {
        [Fact]
        public void Handle_Null_Parms_Fails()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Handle<FakeMessage>(Substitute.For<IWorkGroup>(),
                    null, null, null, null);
            });
        }

        [Fact]
        public void Handle_Message()
        {
            Action<IReceivedMessage<FakeMessage>, IWorkerNotification> action = (message, notification) => {};

            var factory = Substitute.For<ITaskFactory>();
            factory.Scheduler.Returns(Substitute.For<ATaskScheduler>());
            Task blah;
            factory.TryStartNew(null, null, null, out blah).ReturnsForAnyArgs(TryStartNewResult.Added);

            var test = Create();
            test.Handle(null, Substitute.For<IReceivedMessage<FakeMessage>>(),
                Substitute.For<IWorkerNotification>(),
                action,
                factory);
        }

        private SchedulerMessageHandler Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<SchedulerMessageHandler>();
        }
    }
}
