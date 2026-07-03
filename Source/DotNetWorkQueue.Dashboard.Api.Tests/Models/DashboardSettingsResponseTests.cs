using DotNetWorkQueue.Dashboard.Api.Models;
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
            Assert.IsTrue(sut.ReadOnly);
        }

        [TestMethod]
        public void ReadOnly_Defaults_To_False()
        {
            var sut = new DashboardSettingsResponse();
            Assert.IsFalse(sut.ReadOnly);
        }
    }
}
