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
using DotNetWorkQueue.Tests.IoC;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests
{
    [Collection("IoC")]
    public class QueueCreatorTests
    {
        [Theory, AutoData]
        public void Create_Null_Services_Fails(string queue, string connection)
        {
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(
                            queue, connection);
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateProducer(string queue, string connection)
        {
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateProducer<FakeMessage>(
                    queue, connection);
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumer(string queue, string connection)
        {
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumer(queue, connection);
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueScheduler(string queue, string connection)
        {
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerQueueScheduler(queue, connection);
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueSchedulerWithFactory(string queue, string connection)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<ITaskFactory>();
            factory.Scheduler.Returns(fixture.Create<ATaskScheduler>());

            var workGroup = fixture.Create<IWorkGroup>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerQueueScheduler(queue, connection, factory, workGroup);
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerAsync(string queue, string connection)
        {
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerAsync(queue, connection);
            }
        }
    }
}
