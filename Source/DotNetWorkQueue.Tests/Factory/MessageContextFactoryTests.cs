using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.Factory
{
    /// <summary>
    /// The same shortcut, and the same safety net, as
    /// <see cref="WorkerNotificationFactoryTests"/>: build the default directly, defer to the
    /// container for anything else.
    /// </summary>
    [TestClass]
    public class MessageContextFactoryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            //the original coverage: an auto-built factory still produces something
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = fixture.Create<MessageContextFactory>();
            Assert.IsNotNull(factory.Create());
        }

        [TestMethod]
        public void Builds_The_Default_Implementation()
        {
            using var container = NewContainer();
            container.Register<IMessageContext, MessageContext>(Lifestyle.Transient);

            var test = Create(container);

            using var context = test.Create();
            Assert.IsInstanceOfType<MessageContext>(context);
        }

        [TestMethod]
        public void Defers_To_The_Container_When_The_Registration_Was_Replaced()
        {
            using var container = NewContainer();
            container.Register<IMessageContext>(() => Substitute.For<IMessageContext>(), Lifestyle.Transient);

            var test = Create(container);

            using var context = test.Create();
            Assert.IsNotInstanceOfType<MessageContext>(context,
                "a replaced registration has to win");
        }

        [TestMethod]
        public void Produces_A_New_Instance_Each_Time()
        {
            //it holds the state of one message, so it must never be shared
            using var container = NewContainer();
            container.Register<IMessageContext, MessageContext>(Lifestyle.Transient);

            var test = Create(container);

            using var first = test.Create();
            using var second = test.Create();
            Assert.AreNotSame(first, second);
        }

        [TestMethod]
        public void Gives_Each_Context_Its_Own_Worker_Notification()
        {
            using var container = NewContainer();
            container.Register<IMessageContext, MessageContext>(Lifestyle.Transient);

            var notificationFactory = Substitute.For<IWorkerNotificationFactory>();
            notificationFactory.Create().Returns(_ => Substitute.For<IWorkerNotification>());

            var test = Create(container, notificationFactory);

            using var first = test.Create();
            using var second = test.Create();
            Assert.AreNotSame(first.WorkerNotification, second.WorkerNotification);
        }

        /// <summary>
        /// A container configured the way the library configures its own: auto-verification off,
        /// which is what lets a disposable transient like the context be registered at all.
        /// </summary>
        private static Container NewContainer()
        {
            var container = new Container();
            container.Options.EnableAutoVerification = false;
            return container;
        }

        private static MessageContextFactory Create(Container container,
            IWorkerNotificationFactory notificationFactory = null)
        {
            var containerFactory = Substitute.For<IContainerFactory>();
            containerFactory.Create().Returns(new ContainerWrapper(container));

            return new MessageContextFactory(containerFactory,
                notificationFactory ?? Substitute.For<IWorkerNotificationFactory>());
        }
    }
}
