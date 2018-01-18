using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class StateInformationTests
    {
        [Fact]
        public void Create_Null_Constructor_Ok()
        {
            var test = new StateInformation(null);
            Assert.Null(test.Group);
        }
        [Fact]
        public void Create_With_WorkGroup()
        {
            var group = Substitute.For<IWorkGroup>();
            var test = new StateInformation(group);
            Assert.Equal(group, test.Group);
        }
    }
}
