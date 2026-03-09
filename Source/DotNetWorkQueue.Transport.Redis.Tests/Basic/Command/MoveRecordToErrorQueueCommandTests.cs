using System;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class MoveRecordToErrorQueueCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var error = new Exception();
            var context = Substitute.For<IMessageContext>();
            var test = new MoveRecordToErrorQueueCommand<string>(error, number, context);
            Assert.AreEqual(number, test.QueueId);
            Assert.AreEqual(error, test.Exception);
            Assert.AreEqual(context, test.MessageContext);
        }
    }
}
