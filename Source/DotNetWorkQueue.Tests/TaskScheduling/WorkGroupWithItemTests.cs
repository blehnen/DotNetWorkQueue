using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class WorkGroupWithItemTests
    {
        [TestMethod]
        public void Test()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var group = fixture.Create<IWorkGroup>();
            var counter = fixture.Create<ICounter>();
            group.ConcurrencyLevel.Returns(5);
            fixture.Inject(group);
            fixture.Inject(counter);
        }
    }
}
