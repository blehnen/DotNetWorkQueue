using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using NSubstitute;



using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class ProducerQueueRpcTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Get_ReadOnlyConfiguration_Set_After_First_Message()
        {
            var test = CreateQueue();
            var response = test.CreateResponse(Substitute.For<IMessageId>(),
                    TimeSpan.FromHours(1));
            test.Send(new FakeMessage(), response);
            Assert.True(test.Configuration.IsReadOnly);
        }

        [Fact]
        public void Send_Null_Message_Exception()
        {
            using (var test = CreateQueue())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.Send(null, null);
                    });
            }
        }

        [Fact]
        public void Send_Null_ResponseID_Exception()
        {
            using (var test = CreateQueue())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        test.Send(new FakeMessage(), null);
                    });
            }
        }

        [Fact]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                var response = test.CreateResponse(Substitute.For<IMessageId>(),
                    TimeSpan.FromHours(1));
                test.Send(new FakeMessage(), response);
            }
        }

        [Fact]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                var response = test.CreateResponse(Substitute.For<IMessageId>(),
                   TimeSpan.FromHours(1));
                test.Send(new FakeMessage(), response);
            }
        }

        [Theory, AutoData]
        public void Send_Message_And_Data(string value)
        {
            using (var test = CreateQueue())
            {
                var data = new FakeAMessageData();
                IMessageContextData<string> header = new MessageContextData<string>(value, string.Empty);
                data.SetHeader(header, value);
                var response = test.CreateResponse(Substitute.For<IMessageId>(),
                    TimeSpan.FromHours(1));
                test.Send(new FakeMessage(), response, data);
            }
        }
        private ProducerQueueRpc<FakeMessage> CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var responseIdFactory = fixture.Create<IResponseIdFactory>();
            var messageId = fixture.Create<IMessageId>();
            messageId.Id.Value.Returns(100);
            responseIdFactory.Create(null, TimeSpan.FromHours(1)).ReturnsForAnyArgs(new ResponseId(messageId, TimeSpan.FromHours(1)));
            fixture.Inject(responseIdFactory);

            return fixture.Create<ProducerQueueRpc<FakeMessage>>();
        }

        public class FakeMessage
        {

        }
    }
}
