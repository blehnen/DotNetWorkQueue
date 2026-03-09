using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;



using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ProducerQueueTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Get_ReadOnlyConfiguration_Set_After_First_Message()
        {
            var test = CreateQueue();
            test.Send(new FakeMessage());
            Assert.IsTrue(test.Configuration.IsReadOnly);
        }

        [TestMethod]
        public void Send_Null_Message_Exception()
        {
            using (var test = CreateQueue())
            {
                Assert.ThrowsExactly<ArgumentNullException>(
                    delegate
                    {
                        FakeMessage message = null;
                        // ReSharper disable once ExpressionIsAlwaysNull
                        test.Send(message);
                    });
            }
        }

        [TestMethod]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
            }
        }

        [TestMethod]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
            }
        }

        [TestMethod]
        public void Send_Message_And_Data()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
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

        [TestClass]

        public class FakeMessage
        {

        }
    }
}
