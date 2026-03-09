using System;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class MessageHandlerAsyncTests
    {
        [TestMethod]
        public async Task Test_Handle_Null_Arguments_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerAsync>();
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => test.HandleAsync(null, null));
        }
    }
}
