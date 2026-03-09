using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageHandlerTests
    {
        [TestMethod]
        public void Test_Handle_Null_Arguments_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandler>();
            Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                test.Handle(null, null);
            });
        }
    }
}
