using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageTests
    {
        [TestMethod]
        public void Create_Null_Constructor_Headers_Ok()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var test = new Message(data, null);
            Assert.IsNotNull(test);
        }
        [TestMethod]
        public void GetSet_Header_Raw()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headers = fixture.Create<Dictionary<string, object>>();
            var test = new Message(data, headers);

            Assert.AreEqual(headers.Count, test.Headers.Count);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)test.Headers, (System.Collections.ICollection)headers);
            Assert.AreNotSame(headers, test.Headers);
        }
        [TestMethod]
        public void GetSet_Header()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headerData = fixture.Create<HeaderData>();
            var value = fixture.Create<string>();
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData);
        }
        [TestMethod]
        public void GetSet_InternalHeader()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headerData = fixture.Create<HeaderData>();
            var value = fixture.Create<string>();
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetInternalHeader(property, headerData);

            var headerData2 = test.GetInternalHeader(property);
            Assert.AreEqual(headerData2, headerData);
        }

        [TestMethod]
        public void GetSet_Headers_Equal()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headerData = fixture.Create<HeaderData>();
            var value = fixture.Create<string>();
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var property = messageContextDataFactory.Create(value, headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData);
        }

        [TestMethod]
        public void GetSet_HeaderInternal_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headerData = fixture.Create<HeaderData>();
            var value = fixture.Create<string>();
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            messageContextDataFactory.Create(value, headerData).Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);
            var headerData2 = test.GetInternalHeader(property);
            Assert.AreEqual(headerData2, headerData);

            var headerData3 = test.GetInternalHeader(property);
            Assert.AreEqual(headerData2, headerData3);
        }

        [TestMethod]
        public void GetSet_Headers_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headerData = fixture.Create<HeaderData>();
            var value = fixture.Create<string>();
            var test = new Message(data);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            messageContextDataFactory.Create(value, headerData).Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);
            var headerData2 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData);

            var headerData3 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData3);
        }

        public class Data
        {

        }
        public class HeaderData
        { }
    }
}
