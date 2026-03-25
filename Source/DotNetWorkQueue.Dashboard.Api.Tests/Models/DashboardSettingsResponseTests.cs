using DotNetWorkQueue.Dashboard.Api.Models;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Models
{
    [TestClass]
    public class DashboardSettingsResponseTests
    {
        [TestMethod]
        public void ReadOnly_Can_Be_Set_And_Read()
        {
            var sut = new DashboardSettingsResponse { ReadOnly = true };
            sut.ReadOnly.Should().BeTrue();
        }

        [TestMethod]
        public void ReadOnly_Defaults_To_False()
        {
            var sut = new DashboardSettingsResponse();
            sut.ReadOnly.Should().BeFalse();
        }
    }
}
