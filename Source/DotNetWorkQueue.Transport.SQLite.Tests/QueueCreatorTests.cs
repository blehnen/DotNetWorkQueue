using System;
using System.IO;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    [TestClass]
    public class QueueCreatorTests
    {
        private readonly string _goodConnection;

        public QueueCreatorTests()
        {
            _goodConnection = $@"Data Source={Path.GetTempPath()}\test.db;Version=3;";
        }

        [TestMethod]
        public void Create_Null_Services_Fails()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>(null))
            {
                Assert.ThrowsExactly<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, _goodConnection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateProducer()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateProducer<FakeMessage>(new QueueConnection(queue, _goodConnection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumer()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumer(new QueueConnection(queue, _goodConnection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueScheduler()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueSchedulerWithFactory()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = "TestQueue";
            var factory = fixture.Create<ITaskFactory>();
            factory.Scheduler.Returns(fixture.Create<ATaskScheduler>());

            var workGroup = fixture.Create<IWorkGroup>();
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection), factory, workGroup);
            }
        }

        [TestMethod]
        public void Create_CreateConsumerAsync()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerAsync(new QueueConnection(queue, _goodConnection));
            }
        }

        [TestMethod]
        public void Create_CreateAdminContainerAsync()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateAdminContainer(new QueueConnection(queue, _goodConnection));
            }
        }
    }
}
