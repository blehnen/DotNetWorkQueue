using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class SettingTests
    {
        [TestMethod]
        public void Get_Value_Equals()
        {
            var setting = new FakeSetting();
            var test = new Setting<FakeSetting>(setting);
            Assert.AreEqual(test.Value, setting);
        }

        private class FakeSetting
        {

        }
    }
}
