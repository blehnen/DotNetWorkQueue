using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class RegisterMessagesTests
    {
        [Fact]
        public void Register()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var messageHandlerRegistration = fixture.Create<IMessageHandlerRegistration>();
            fixture.Inject(messageHandlerRegistration);
            var test = fixture.Create<RegisterMessages>();

            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
            {
            }

            test.Register((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);

            messageHandlerRegistration.Received(1).Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
        }
    }
}
