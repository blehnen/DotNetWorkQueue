using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageContextTests
    {
        [Theory, AutoData]
        public void GetSet_AdditionalContextData(string name)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(name, headerData);
            test.Set(property, headerData);

            var headerData2 = test.Get(property);
            Assert.Equal(headerData2, headerData);
        }
        [Theory, AutoData]
        public void GetSet_AdditionalContextData_Default_Value(string name)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create(name, headerData).Returns(new MessageContextData<HeaderData>(name, headerData));

            var property = messageContextDataFactory.Create(name, headerData);
            var headerData2 = test.Get(property);
            Assert.Equal(headerData2, headerData);

            var headerData3 = test.Get(property);
            Assert.Equal(headerData2, headerData3);
        }

        [Fact]
        public void WorkerNotification_NotNull()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.NotNull(test.WorkerNotification);
        }

        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            Assert.False(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Commit_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.RaiseCommit();
            });
        }
        [Fact]
        public void Disposed_Instance_Rollback_Exception()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.RaiseRollback();
            });
        }
        [Theory, AutoData]
        public void Disposed_Instance_SetAdditionalContextData_Exception(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Set(property, headerData);
            });
        }
        [Theory, AutoData]
        public void Disposed_Instance_GetAdditionalContextData_Exception(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageContext>();
            var messageContextDataFactory =
              fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            var property = messageContextDataFactory.Create(value, headerData);
            test.Set(property, headerData);
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Get(property);
            });
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public class HeaderData : IDisposable
        {
            [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
            public void Dispose()
            {
                
            }
        }
    }
}
