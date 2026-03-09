using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Tests.IoC;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests
{
    [TestClass]
    public class QueueCreatorTests
    {
        [TestMethod]
        public void Create_Null_Services_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>(null))
            {
                Assert.ThrowsExactly<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, connection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateProducer()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateProducer<FakeMessage>(new QueueConnection(queue, connection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumer()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumer(new QueueConnection(queue, connection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueScheduler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, connection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueSchedulerWithFactory()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            var factory = fixture.Create<ITaskFactory>();
            factory.Scheduler.Returns(fixture.Create<ATaskScheduler>());

            var workGroup = fixture.Create<IWorkGroup>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, connection), factory, workGroup);
            }
        }

        [TestMethod]
        public void Create_CreateConsumerAsync()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateConsumerAsync(new QueueConnection(queue, connection));
            }
        }

        [TestMethod]
        public void Create_CreateAdminContainerAsync()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
            var connection = fixture.Create<string>();
            using (var test = new QueueContainer<CreateContainerTest.NoOpDuplexTransport>())
            {
                test.CreateAdminContainer(new QueueConnection(queue, connection));
            }
        }
    }
}
