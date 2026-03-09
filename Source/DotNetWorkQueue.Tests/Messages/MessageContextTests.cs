using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageContextTests
    {
        [TestMethod]
        public void GetSet_AdditionalContextData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();

            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(name, headerData);
            test.Set(property, headerData);

            var headerData2 = test.Get(property);
            Assert.AreEqual(headerData2, headerData);
        }
        [TestMethod]
        public void GetSet_AdditionalContextData_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var name = fixture.Create<string>();
            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create(name, headerData).Returns(new MessageContextData<HeaderData>(name, headerData));

            var property = messageContextDataFactory.Create(name, headerData);
            var headerData2 = test.Get(property);
            Assert.AreEqual(headerData2, headerData);

            var headerData3 = test.Get(property);
            Assert.AreEqual(headerData2, headerData3);
        }

        [TestMethod]
        public void WorkerNotification_NotNull()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.IsNotNull(test.WorkerNotification);
        }

        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.IsFalse(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Commit_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.RaiseCommit();
            });
        }
        [TestMethod]
        public void Disposed_Instance_Rollback_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.RaiseRollback();
            });
        }
        [TestMethod]
        public void Disposed_Instance_SetAdditionalContextData_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Set(property, headerData);
            });
        }
        [TestMethod]
        public void Disposed_Instance_GetAdditionalContextData_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<string>();
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Set(property, headerData);
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.Get(property);
            });
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        [TestClass]
        public class HeaderData : IDisposable
        {
            [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
            public void Dispose()
            {

            }
        }
    }
}
