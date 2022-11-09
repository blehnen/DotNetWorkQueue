using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageHandlerAsyncTests
    {
        [Fact]
        public void Test_Handle_Null_Arguments_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerAsync>();
            Assert.ThrowsAsync<ArgumentNullException>(() => test.HandleAsync(null, null));
        }
    }
}
