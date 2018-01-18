using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Tests.IoC;
using NSubstitute;



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
