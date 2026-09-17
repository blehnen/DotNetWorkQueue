using System;
using System.IO;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.SQLite.Basic;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    [TestClass]
    public class QueueCreatorTests
    {
        //Nothing creates this queue, and since #348 a producer or consumer refuses to be built for a
        //queue that is not there. What these cover is therefore the refusal; that the same calls
        //succeed against a queue that exists is covered by QueueMustExistTests, which has a real one.
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
                Assert.ThrowsExactly<QueueDoesNotExistException>(
                    // ReSharper disable once AccessToDisposedClosure
                    () => test.CreateProducer<FakeMessage>(new QueueConnection(queue, _goodConnection)));
            }
        }

        [TestMethod]
        public void Create_CreateConsumer()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                Assert.ThrowsExactly<QueueDoesNotExistException>(
                    // ReSharper disable once AccessToDisposedClosure
                    () => test.CreateConsumer(new QueueConnection(queue, _goodConnection)));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueScheduler()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                Assert.ThrowsExactly<QueueDoesNotExistException>(
                    // ReSharper disable once AccessToDisposedClosure
                    () => test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection)));
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
                Assert.ThrowsExactly<QueueDoesNotExistException>(
                    // ReSharper disable once AccessToDisposedClosure
                    () => test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection), factory, workGroup));
            }
        }

        [TestMethod]
        public void Create_CreateConsumerAsync()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                Assert.ThrowsExactly<QueueDoesNotExistException>(
                    // ReSharper disable once AccessToDisposedClosure
                    () => test.CreateConsumerAsync(new QueueConnection(queue, _goodConnection)));
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
