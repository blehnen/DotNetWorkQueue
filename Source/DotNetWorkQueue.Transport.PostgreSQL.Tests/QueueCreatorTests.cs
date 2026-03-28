using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using NSubstitute;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests
{
    [TestClass]
    public class QueueCreatorTests
    {
        private const string GoodConnection =
            "Server=localhost;Application Name=Consumer;Database=db;User ID=sa;Password=password";

        [TestMethod]
        public void Create_Null_Services_Fails()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>(null))
            {
                Assert.ThrowsExactly<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateProducer()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateConsumer()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumer(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateConsumerQueueScheduler()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerQueueScheduler(new QueueConnection(queue, GoodConnection));
                    });
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
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerQueueScheduler(new QueueConnection(queue, GoodConnection), factory, workGroup);
                    });
            }
        }

        [TestMethod]
        public void Create_CreateConsumerAsync()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerAsync(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [TestMethod]
        public void Create_CreateAdminContainerAsync()
        {
            var queue = "TestQueue";
            using (var test = new QueueContainer<PostgreSqlMessageQueueInit>())
            {
                Assert.ThrowsExactly<Npgsql.NpgsqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateAdminContainer(new QueueConnection(queue, GoodConnection));
                    });
            }
        }
    }
}
