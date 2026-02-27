using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SqlServer.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests
{
    [Collection("IoC")]
    public class QueueCreatorTests
    {
        private const string GoodConnection =
            "Server=localhost;Application Name=Consumer;Database=db;User ID=sa;Password=password";

        [Theory, AutoData]
        public void Create_Null_Services_Fails(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateProducer(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumer(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumer(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueScheduler(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerQueueScheduler(new QueueConnection(queue, GoodConnection));
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueSchedulerWithFactory(string queue)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<ITaskFactory>();
            factory.Scheduler.Returns(fixture.Create<ATaskScheduler>());

            var workGroup = fixture.Create<IWorkGroup>();
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerQueueScheduler(new QueueConnection(queue, GoodConnection), factory, workGroup);
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerAsync(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateConsumerAsync(new QueueConnection(queue, GoodConnection));
                    });
               
            }
        }

        [Theory, AutoData]
        public void Create_CreateAdminContainerAsync(string queue)
        {
            using (var test = new QueueContainer<SqlServerMessageQueueInit>())
            {
                Assert.Throws<Microsoft.Data.SqlClient.SqlException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateAdminContainer(new QueueConnection(queue, GoodConnection));
                    });
               
            }
        }
    }
}
