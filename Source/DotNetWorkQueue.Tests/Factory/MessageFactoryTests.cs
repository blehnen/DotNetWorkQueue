using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class MessageFactoryTests
    {
        [TestMethod]
        public void Create_Message()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headers = fixture.Create<Dictionary<string, object>>();
            var factory = Create();
            var test = factory.Create(data, headers);
            Assert.AreEqual(test.Body, data);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)test.Headers, (System.Collections.ICollection)headers);
        }

        [TestMethod]
        public void GetSet_Header()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headers = fixture.Create<Dictionary<string, object>>();
            var headerData = fixture.Create<HeaderData>();
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetHeader(property, headerData);

            var headerData2 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData);
        }
        [TestMethod]
        public void GetSet_InternalHeader()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var data = fixture.Create<Data>();
            var headers = fixture.Create<Dictionary<string, object>>();
            var headerData = fixture.Create<HeaderData>();
            var factory = Create();
            var test = factory.Create(data, headers);

            var messageContextDataFactory = CreateDataFactory();

            var property = messageContextDataFactory.Create("Test", headerData);
            test.SetInternalHeader(property, headerData);

            var headerData2 = test.GetInternalHeader(property);
            Assert.AreEqual(headerData2, headerData);
        }

        private IMessageFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageFactory>();
        }

        private IMessageContextDataFactory CreateDataFactory()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IMessageContextDataFactory>();
        }
        public class Data
        {

        }
        public class HeaderData
        { }
    }
}
