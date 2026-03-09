using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.PostgreSQL.Basic;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
            var queue = fixture.Create<string>();
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var queue = fixture.Create<string>();
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
