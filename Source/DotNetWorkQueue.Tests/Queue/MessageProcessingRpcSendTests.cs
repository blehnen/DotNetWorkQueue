using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageProcessingRpcSendTests
    {
        [Fact]
        public void Run_No_Data()
        {
            var test = Create();
            test.Handle(new FakeMessage(), TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public void Run_Data()
        {
            var test = Create();
            test.Handle(new FakeMessage(), new FakeAMessageData(),TimeSpan.FromMilliseconds(1000));
        }

        public
            MessageProcessingRpcSend<FakeMessage> Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var sentMessage = fixture.Create<ISentMessage>();
            fixture.Inject(sentMessage);

            var output = fixture.Create<QueueOutputMessage>();
            var sendMessages = fixture.Create<ISendMessages>();
            sendMessages.Send(fixture.Create<IMessage>(),
                  fixture.Create<IAdditionalMessageData>()).ReturnsForAnyArgs(output);
            fixture.Inject(sendMessages);

            fixture.Inject(fixture.Create<ProducerQueue<FakeMessage>>());
            return fixture.Create<MessageProcessingRpcSend<FakeMessage>>();
        }
    }
}
