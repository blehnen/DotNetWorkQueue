using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;



using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class ProducerQueueTests
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
            test.Send(new FakeMessage());
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
                        FakeMessage message = null;
                        // ReSharper disable once ExpressionIsAlwaysNull
                        test.Send(message);
                    });
            }
        }

        [Fact]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
            }
        }

        [Fact]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
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
                test.Send(new FakeMessage(), data);
            }
        }

        private ProducerQueue
                    <FakeMessage> CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ProducerQueue<FakeMessage>>();
        }

        public class FakeMessage
        {

        }
    }
}
