using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class SendHeartBeatCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var test = new SendHeartBeatCommand<string>(number);
            Assert.AreEqual(number, test.QueueId);
        }
    }
}
