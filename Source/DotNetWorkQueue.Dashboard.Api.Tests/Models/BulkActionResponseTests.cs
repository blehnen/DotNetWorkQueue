using DotNetWorkQueue.Dashboard.Api.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Models
{
    [TestClass]
    public class BulkActionResponseTests
    {
        [TestMethod]
        public void Count_Can_Be_Set_And_Read()
        {
            var sut = new BulkActionResponse { Count = 150L };
            sut.Count.Should().Be(150L);
        }

        [TestMethod]
        public void Count_Defaults_To_Zero()
        {
            var sut = new BulkActionResponse();
            sut.Count.Should().Be(0L);
        }
    }
}
