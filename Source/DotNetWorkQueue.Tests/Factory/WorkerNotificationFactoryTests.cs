using System;
using System.Diagnostics;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.Factory
{
    /// <summary>
    /// The factory builds the default implementation directly instead of resolving it, because a
    /// resolve costs about twenty times what the constructor does and one of these is created per
    /// message consumed.
    /// </summary>
    /// <remarks>
    /// The important test here is the second one. SQL Server and PostgreSQL both replace the
    /// <see cref="IWorkerNotification"/> registration with a delegate that returns a relational
    /// variant when the de-queue transaction is held open, and the inbox feature reads that
    /// variant back off the context. If the shortcut ignored a replaced registration those
    /// transports would silently get a plain notification and the feature would stop working
    /// without failing.
    /// </remarks>
    [TestClass]
    public class WorkerNotificationFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            //the original coverage: an auto-built factory still produces something
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<WorkerNotificationFactory>();
            Assert.IsNotNull(factory.Create());
        }

        [TestMethod]
        public void Builds_The_Default_Implementation()
        {
            using var container = NewContainer();
            container.Register<IWorkerNotification, WorkerNotification>(Lifestyle.Transient);

            var test = Create(container);

            Assert.IsInstanceOfType<WorkerNotification>(test.Create());
        }

        [TestMethod]
        public void Defers_To_The_Container_When_The_Registration_Was_Replaced()
        {
            using var container = NewContainer();
            container.Register<IWorkerNotification>(() => new Replacement(), Lifestyle.Transient);

            var test = Create(container);

            Assert.IsInstanceOfType<Replacement>(test.Create(),
                "a replaced registration has to win; SQL Server and PostgreSQL rely on it");
        }

        [TestMethod]
        public void Produces_A_New_Instance_Each_Time()
        {
            //it carries per-message state (the heartbeat), so it must never be shared
            using var container = NewContainer();
            container.Register<IWorkerNotification, WorkerNotification>(Lifestyle.Transient);

            var test = Create(container);

            Assert.AreNotSame(test.Create(), test.Create());
        }

        private static Container NewContainer()
        {
            var container = new Container();
            container.Options.EnableAutoVerification = false;
            return container;
        }

        private static WorkerNotificationFactory Create(Container container)
        {
            var containerFactory = Substitute.For<IContainerFactory>();
            containerFactory.Create().Returns(new ContainerWrapper(container));

            return new WorkerNotificationFactory(containerFactory,
                Substitute.For<IHeaders>(),
                Substitute.For<IQueueCancelWork>(),
                new TransportConfigurationReceive(Substitute.For<IConnectionInformation>(),
                    Substitute.For<IQueueDelayFactory>(), Substitute.For<IRetryDelayFactory>()),
                Substitute.For<ILogger>(),
                Substitute.For<IMetrics>(),
                new ActivitySource("test"));
        }

        private sealed class Replacement : IWorkerNotification
        {
            public ICancelWork WorkerStopping { get; set; }
            public IWorkerHeartBeatNotification HeartBeat { get; set; }
            public IHeaders HeaderNames { get; set; }
            public bool TransportSupportsRollback { get; set; }
            public ILogger Log { get; set; }
            public IMetrics Metrics { get; set; }
            public ActivitySource Tracer { get; set; }
            public IMessageCancellation MessageCancellation { get; set; }
        }
    }
}
