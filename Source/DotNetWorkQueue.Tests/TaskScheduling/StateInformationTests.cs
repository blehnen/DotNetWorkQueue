using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class StateInformationTests
    {
        [TestMethod]
        public void Create_Null_Constructor_Ok()
        {
            var test = new StateInformation(null);
            Assert.IsNull(test.Group);
        }
        [TestMethod]
        public void Create_With_WorkGroup()
        {
            var group = Substitute.For<IWorkGroup>();
            var test = new StateInformation(group);
            Assert.AreEqual(group, test.Group);
        }
    }
}
