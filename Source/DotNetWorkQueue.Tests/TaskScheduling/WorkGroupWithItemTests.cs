using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class WorkGroupWithItemTests
    {
        [Fact]
        public void Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var group = fixture.Create<IWorkGroup>();
            var counter = fixture.Create<ICounter>();
            group.ConcurrencyLevel.Returns(5);
            group.MaxQueueSize.Returns(1);
            fixture.Inject(group);
            fixture.Inject(counter);
        }
    }
}
