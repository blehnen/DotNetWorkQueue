using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageProcessingRpcReceiveTests
    {
        [Fact]
        public void Create_Handle_Fails()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Handle(null, TimeSpan.MinValue, null);
            });
        }

        [Fact]
        public void Create_Handle_Fails2()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Handle(Substitute.For<IMessageId>(), TimeSpan.MinValue, null);
            });
        }

        [Fact]
        public void Create_Default()
        {
            var test = Create();
            test.Handle(Substitute.For<IMessageId>(), TimeSpan.FromMilliseconds(1000),
                Substitute.For<IQueueWait>());
        }

        public MessageProcessingRpcReceive<FakeMessage> Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var receiveMessages = fixture.Create<IReceiveMessagesFactory>();
            fixture.Inject(receiveMessages);
            var fakeMessage = fixture.Create<IMessage>();
            fakeMessage.Body.Returns(new FakeMessage());
            fixture.Inject(fakeMessage);
            ICommitMessage commitMessage = fixture.Create<CommitMessage>();
            fixture.Inject(commitMessage);

            IReceivedMessageInternal message = fixture.Create<ReceivedMessageInternal>();

            var messageHandlerRegistration = fixture.Create<IMessageHandlerRegistration>();
            messageHandlerRegistration.GenerateMessage(message)
                   .Returns(new ReceivedMessage<FakeMessage>(message));
            fixture.Inject(messageHandlerRegistration);

            receiveMessages.Create().ReceiveMessage(null).ReturnsForAnyArgs(message);

            return fixture.Create<MessageProcessingRpcReceive<FakeMessage>>();
        }
    }
}
