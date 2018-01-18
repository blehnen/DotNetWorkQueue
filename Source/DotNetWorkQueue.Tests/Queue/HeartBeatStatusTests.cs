using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class HeartBeatStatusTests
    {
        [Fact]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new HeartBeatStatus(messageId, null);
            Assert.Equal(messageId, test.MessageId);
        }
        [Fact]
        public void Get_LastHeartBeatTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var time = DateTime.Now;
            var test = new HeartBeatStatus(messageId, time);
            Assert.Equal(time, test.LastHeartBeatTime);
        }
        [Fact]
        public void Get_LastHeartBeatTime_Null()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new HeartBeatStatus(messageId, null);
            Assert.Null(test.LastHeartBeatTime);
        }
    }
}
