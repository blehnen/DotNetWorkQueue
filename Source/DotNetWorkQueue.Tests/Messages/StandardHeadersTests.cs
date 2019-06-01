using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class StandardHeadersTests
    {
        private IStandardHeaders Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<StandardHeaders>();
        }
    }
}
