using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;



using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class ProducerMethodQueueTests
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
            test.Send((message, notification) => Console.WriteLine("hello"));
            Assert.True(test.Configuration.IsReadOnly);
        }

        [Fact]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                test.Send((message, notification) => Console.WriteLine("hello"));
            }
        }

        [Fact]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                test.Send((message, notification) => Console.WriteLine("hello"));
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
                test.Send((message, notification) => Console.WriteLine("hello"), data);
            }
        }

        private ProducerMethodQueue CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(CreateBaseQueue(fixture));
            return fixture.Create<ProducerMethodQueue>();
        }
        private IProducerQueue<MessageExpression> CreateBaseQueue(IFixture fixture)
        {
            return fixture.Create<ProducerQueue<MessageExpression>>();
        }
    }
}
