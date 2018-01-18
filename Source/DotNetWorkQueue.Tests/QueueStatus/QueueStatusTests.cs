using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.QueueStatus;


using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueStatusTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var providers = new List<IQueueStatusProvider> {fixture.Create<QueueStatusProviderNoOp>()};
            fixture.Inject(providers);
            var test = fixture.Create<DotNetWorkQueue.QueueStatus.QueueStatus>();
            Assert.NotEmpty(test.Queues);
        }
    }
}
