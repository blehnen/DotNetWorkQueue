using System;
using System.IO;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.SQLite.Basic;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests
{
    [Collection("IoC")]
    public class QueueCreatorTests
    {
        private readonly string _goodConnection;

        public QueueCreatorTests()
        {
            _goodConnection = $@"Data Source={Path.GetTempPath()}\test.db;Version=3;";
        }

        [Theory, AutoData]
        public void Create_Null_Services_Fails(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateProducer<FakeMessage>(new QueueConnection(queue, _goodConnection));
                    });
            }
        }

        [Theory, AutoData]
        public void Create_CreateProducer(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateProducer<FakeMessage>(new QueueConnection(queue, _goodConnection));
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumer(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumer(new QueueConnection(queue, _goodConnection));
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueScheduler(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection));
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerQueueSchedulerWithFactory(string queue)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<ITaskFactory>();
            factory.Scheduler.Returns(fixture.Create<ATaskScheduler>());

            var workGroup = fixture.Create<IWorkGroup>();
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerQueueScheduler(new QueueConnection(queue, _goodConnection), factory, workGroup);
            }
        }

        [Theory, AutoData]
        public void Create_CreateConsumerAsync(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateConsumerAsync(new QueueConnection(queue, _goodConnection));
            }
        }

        [Theory, AutoData]
        public void Create_CreateAdminContainerAsync(string queue)
        {
            using (var test = new QueueContainer<SqLiteMessageQueueInit>())
            {
                test.CreateAdminContainer(new QueueConnection(queue, _goodConnection));
            }
        }
    }
}
