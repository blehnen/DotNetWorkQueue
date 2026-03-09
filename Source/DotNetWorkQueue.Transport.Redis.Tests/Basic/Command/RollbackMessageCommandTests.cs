using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class RollbackMessageCommandTests
    {
        [TestMethod]
        public void Create_Null_Constructor_Time_Ok()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var test = new RollbackMessageCommand<string>(null, number, null);
            Assert.IsNotNull(test);
        }
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var test = new RollbackMessageCommand<string>(null, number, null);
            Assert.AreEqual(number, test.QueueId);
            Assert.IsNull(test.IncreaseQueueDelay);

            TimeSpan? time = TimeSpan.MinValue;
            test = new RollbackMessageCommand<string>(null, number, time);
            Assert.AreEqual(number, test.QueueId);
            Assert.AreEqual(time, test.IncreaseQueueDelay);
        }
    }
}
