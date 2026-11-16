using System;
using System.IO;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageHandlerAsyncTests
    {
        [TestMethod]
        public async Task Test_Handle_Null_Arguments_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerAsync>();
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => test.HandleAsync(null, null));
        }

        [TestMethod]
        public void HandleAsync_Does_Not_Wrap_An_Exception_From_The_Handler()
        {
            // ProcessMessageAsync reports whatever reaches it unchanged, so this link must not wrap.
            // The handler is called through a dynamic delegate, which must not add a TargetInvocationException.
            var thrownByHandler = new InvalidDataException("handler failed", new IndexOutOfRangeException("cause"));
            Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task> handler = (message, notification) => throw thrownByHandler;

            var registration = Substitute.For<IMessageHandlerRegistrationAsync>();
            // GetHandler and GenerateMessage return dynamic, so Returns is called statically.
            SubstituteExtensions.Returns<object>(registration.GetHandler(), handler);
            SubstituteExtensions.Returns<object>(registration.GenerateMessage(Arg.Any<IReceivedMessageInternal>()),
                Substitute.For<IReceivedMessage<FakeMessage>>());
            var test = new MessageHandlerAsync(registration);

            var error = Assert.ThrowsExactly<InvalidDataException>(
                () => test.HandleAsync(Substitute.For<IReceivedMessageInternal>(), Substitute.For<IWorkerNotification>()));

            Assert.AreSame(thrownByHandler, error);
        }
    }
}
