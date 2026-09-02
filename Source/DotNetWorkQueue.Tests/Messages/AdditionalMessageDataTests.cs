using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class AdditionalMessageDataTests
    {
        [TestMethod]
        public void SetAndGet_CorrelationId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var correlationId = fixture.Create<ICorrelationId>();
            message.CorrelationId = correlationId;
            Assert.AreEqual(message.CorrelationId, correlationId);
        }

        [TestMethod]
        public void SetAndGet_Route()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var route = fixture.Create<string>();
            message.Route = route;
            Assert.AreEqual(message.Route, route);
        }

        [TestMethod]
        public void SetAndGet_AdditionalMetaData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();
            var messageData = fixture.Create<IAdditionalMetaData>();
            message.AdditionalMetaData.Add(messageData);
            Assert.AreEqual(message.AdditionalMetaData[0], messageData);
        }
        [TestMethod]
        public void SetAndGet_Headers()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
                .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.AreEqual(message.GetHeader(property), headerData);
        }

        [TestMethod]
        public void SetAndGet_Headers_RawAccess()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();

            messageContextDataFactory.Create("Test", headerData)
               .Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            message.SetHeader(property, headerData);

            Assert.AreEqual(message.Headers.Values.First(), headerData);
        }

        [TestMethod]
        public void GetSet_Headers_Default_Value()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<AdditionalMessageData>();

            var messageContextDataFactory =
               fixture.Create<IMessageContextDataFactory>();

            var headerData = fixture.Create<HeaderData>();
            messageContextDataFactory.Create("Test", headerData).Returns(new MessageContextData<HeaderData>("Test", headerData));

            var property = messageContextDataFactory.Create("Test", headerData);
            var headerData2 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData);

            var headerData3 = test.GetHeader(property);
            Assert.AreEqual(headerData2, headerData3);
        }

        //The four collections this class holds are created on first use rather than in the
        //constructor - one of these is built for every message sent, and building them anyway was
        //the single largest allocation in a send. These pin the behaviour that has to survive that.

        [TestMethod]
        public void Headers_Is_Empty_When_None_Were_Set()
        {
            var test = new AdditionalMessageData();
            Assert.IsNotNull(test.Headers);
            Assert.IsEmpty(test.Headers);
        }

        [TestMethod]
        public void Headers_Shows_A_Header_Set_After_It_Was_First_Read()
        {
            //reading first is the case that matters: the empty read must not leave the object
            //believing it has no headers, and the view handed out afterwards must be the real one
            var test = new AdditionalMessageData();
            Assert.IsEmpty(test.Headers);

            var property = new MessageContextData<HeaderData>("Test", null);
            var headerData = new HeaderData();
            test.SetHeader(property, headerData);

            Assert.HasCount(1, test.Headers);
            Assert.AreEqual(headerData, test.Headers.Values.First());
        }

        [TestMethod]
        public void Headers_Held_From_Before_A_Write_Still_Shows_It()
        {
            //the property has always handed back a live view over the header dictionary, and a
            //caller holding one saw a header set afterwards. The dictionary is now created on
            //first write, so the view has to read it rather than wrap it once - this is the
            //behaviour that pins that
            var test = new AdditionalMessageData();
            var held = test.Headers;
            Assert.IsEmpty(held);

            var property = new MessageContextData<HeaderData>("Test", null);
            var headerData = new HeaderData();
            test.SetHeader(property, headerData);

            Assert.HasCount(1, held);
            Assert.IsTrue(held.ContainsKey("Test"));
            Assert.IsTrue(held.TryGetValue("Test", out var value));
            Assert.AreEqual(headerData, value);
            Assert.AreEqual(headerData, held["Test"]);
            CollectionAssert.AreEqual(new[] { "Test" }, held.Keys.ToList());
            Assert.AreEqual(headerData, held.Values.Single());
            Assert.AreEqual("Test", held.First().Key);
        }

        [TestMethod]
        public void TryGetSetting_Returns_False_When_None_Were_Set()
        {
            //asked on every send - GetJobName looks for "JobName" - so it has to answer without
            //creating the dictionary it is looking in
            var test = new AdditionalMessageData();
            Assert.IsFalse(test.TryGetSetting("Test", out var value));
            Assert.IsNull(value);
        }

        [TestMethod]
        public void SetAndGet_Setting()
        {
            var test = new AdditionalMessageData();
            test.SetSetting("Test", 42);
            Assert.IsTrue(test.TryGetSetting("Test", out var value));
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public void TraceTags_Is_Usable_Without_Being_Set_Up()
        {
            var test = new AdditionalMessageData();
            Assert.IsNotNull(test.TraceTags);
            test.TraceTags["Test"] = "Value";
            Assert.AreEqual("Value", test.TraceTags["Test"]);
        }

        [TestMethod]
        public void AdditionalMetaData_Is_Usable_Without_Being_Set_Up()
        {
            var test = new AdditionalMessageData();
            Assert.IsNotNull(test.AdditionalMetaData);
            Assert.IsEmpty(test.AdditionalMetaData);
        }

        [TestClass]

        public class HeaderData
        {

        }
    }
}
