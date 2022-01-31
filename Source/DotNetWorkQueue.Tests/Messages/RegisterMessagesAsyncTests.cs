using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class RegisterMessagesAsyncTests
    {
        [Fact]
        public void Register()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var messageHandlerRegistration = fixture.Create<IMessageHandlerRegistrationAsync>();
            fixture.Inject(messageHandlerRegistration);
            var test = fixture.Create<RegisterMessagesAsync>();

            Task Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker) => null;
            test.Register((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Action);

            messageHandlerRegistration.Received(1).Set((Func<IReceivedMessage<FakeMessage>, IWorkerNotification, Task>)Action);
        }
    }
}
