using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageProcessingTests
    {
        [Fact]
        public void Handle()
        {
            var wrapper = new MessageProcessingWrapper();
            var test = wrapper.Create();
            test.Handle();
            wrapper.MessageContextFactory.Received(1).Create();
        }

        [Fact]
        public void Handle_Receive_Message()
        {
            var wrapper = new MessageProcessingWrapper();
            var test = wrapper.Create();
            test.Handle();
            wrapper.ReceiveMessages.ReceivedWithAnyArgs(1).ReceiveMessage(null);
        }

        public class MessageProcessingWrapper
        {
            private readonly IFixture _fixture;
            public IReceiveMessages ReceiveMessages;
            public IMessageContextFactory MessageContextFactory;
            public MessageProcessingWrapper()
            {
                _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                ReceiveMessages = _fixture.Create<IReceiveMessages>();
                var factory = _fixture.Create<IReceiveMessagesFactory>();
                factory.Create().ReturnsForAnyArgs(ReceiveMessages);
                _fixture.Inject(factory);
                MessageContextFactory = _fixture.Create<IMessageContextFactory>();
                _fixture.Inject(ReceiveMessages);
                _fixture.Inject(MessageContextFactory);
            }
            public MessageProcessing Create()
            {
                return _fixture.Create<MessageProcessing>();
            }
        }
    }
}
