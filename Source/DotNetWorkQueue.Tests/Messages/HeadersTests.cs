using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class HeadersTests
    {
        [Fact]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var headers = fixture.Create<IStandardHeaders>();
            var customHeaders = fixture.Create<ICustomHeaders>();
            fixture.Inject(headers);
            fixture.Inject(customHeaders);
            var test = fixture.Create<Headers>();

            Assert.Equal(headers, test.StandardHeaders);
            Assert.Equal(customHeaders, test.CustomHeaders);
        }
    }
}
