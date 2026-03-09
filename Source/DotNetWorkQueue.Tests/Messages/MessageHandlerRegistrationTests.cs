using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageHandlerRegistrationTests
    {
        [TestMethod]
        public void Create_Null_Set_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistration>();
            Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                test.Set<FakeMessage>(null);
            });
        }

        [TestMethod]
        public void GetHandler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistration>();

            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
            {
            }

            test.Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action);
            Assert.AreEqual((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>)Action, test.GetHandler());
        }

        [TestMethod]
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

            Assert.IsInstanceOfType<FakeMessage>(message.Body);
        }

        [TestClass]

        public class FakeMessage
        {

        }
    }
}
