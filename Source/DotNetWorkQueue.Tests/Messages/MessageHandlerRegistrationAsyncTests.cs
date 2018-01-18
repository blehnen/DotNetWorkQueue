using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageHandlerRegistrationAsyncTests
    {
        [Fact]
        public void Create_Null_Set_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistrationAsync>();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Set<FakeMessage>(null);
            });
        }

        [Fact]
        public void GetHandler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistrationAsync>();
            Task Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
            test.Set((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Action);
            Assert.Equal((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Action, test.GetHandler());
        }

        [Fact]
        public void GenerateMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IGenerateReceivedMessage gen = fixture.Create<GenerateReceivedMessage>();
            var inputMessage = fixture.Create<IMessage>();
            inputMessage.Body.Returns(new FakeMessage());
            fixture.Inject(gen);
            fixture.Inject(inputMessage);
            var test = fixture.Create<MessageHandlerRegistrationAsync>();
            Task Func(IReceivedMessage<FakeMessage> recMessage, IWorkerNotification worker) => null;
            test.Set((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>) Func);

            IReceivedMessageInternal rec = fixture.Create<ReceivedMessageInternal>();

            var message = test.GenerateMessage(rec);

            Assert.IsAssignableFrom<FakeMessage>(message.Body);
        }

        public class FakeMessage
        {

        }
    }
}
