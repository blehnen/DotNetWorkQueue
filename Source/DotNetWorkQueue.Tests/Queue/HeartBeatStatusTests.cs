using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class HeartBeatStatusTests
    {
        [TestMethod]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new HeartBeatStatus(messageId, null);
            Assert.AreEqual(messageId, test.MessageId);
        }
        [TestMethod]
        public void Get_LastHeartBeatTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var time = DateTime.Now;
            var test = new HeartBeatStatus(messageId, time);
            Assert.AreEqual(time, test.LastHeartBeatTime);
        }
        [TestMethod]
        public void Get_LastHeartBeatTime_Null()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new HeartBeatStatus(messageId, null);
            Assert.IsNull(test.LastHeartBeatTime);
        }
    }
}
