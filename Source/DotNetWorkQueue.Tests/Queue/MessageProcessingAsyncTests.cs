using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class MessageProcessingAsyncTests
    {
        [TestMethod]
        public async Task Handle()
        {
            var wrapper = new MessageProcessingAsyncWrapper();
            var test = wrapper.Create();
            await test.HandleAsync();
            wrapper.MessageContextFactory.Received(1).Create();
        }

        [TestMethod]
        public async Task Handle_Receive_Message()
        {
            var wrapper = new MessageProcessingAsyncWrapper();
            var test = wrapper.Create();
            await test.HandleAsync();
            wrapper.ReceiveMessages.ReceivedWithAnyArgs(1).Create();
        }

        [TestClass]

        public class MessageProcessingAsyncWrapper
        {
            private readonly IFixture _fixture;
            public IReceiveMessagesFactory ReceiveMessages;
            public IMessageContextFactory MessageContextFactory;
            public MessageProcessingAsyncWrapper()
            {
                _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                ReceiveMessages = _fixture.Create<IReceiveMessagesFactory>();
                MessageContextFactory = _fixture.Create<IMessageContextFactory>();
                _fixture.Inject(ReceiveMessages);
                _fixture.Inject(MessageContextFactory);
            }
            public MessageProcessingAsync Create()
            {
                return _fixture.Create<MessageProcessingAsync>();
            }
        }
    }
}
