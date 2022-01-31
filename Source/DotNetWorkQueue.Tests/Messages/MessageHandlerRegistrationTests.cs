﻿using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageHandlerRegistrationTests
    {
        [Fact]
        public void Create_Null_Set_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistration>();
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
            var test = fixture.Create<MessageHandlerRegistration>();

            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
            {
            }

            test.Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
            Assert.Equal((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action, test.GetHandler());
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
            var test = fixture.Create<MessageHandlerRegistration>();

            void Action(IReceivedMessage<FakeMessage> recMessage, IWorkerNotification worker)
            {
            }

            test.Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);

            IReceivedMessageInternal rec = fixture.Create<ReceivedMessageInternal>();

            var message = test.GenerateMessage(rec);

            Assert.IsAssignableFrom<FakeMessage>(message.Body);
        }

        public class FakeMessage
        {

        }
    }
}
