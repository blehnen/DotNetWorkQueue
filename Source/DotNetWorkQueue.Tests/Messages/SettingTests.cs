using DotNetWorkQueue.Messages;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class SettingTests
    {
        [Fact]
        public void Get_Value_Equals()
        {
            var setting = new FakeSetting();
            var test = new Setting<FakeSetting>(setting);
            Assert.Equal(test.Value, setting);
        }

        private class FakeSetting
        {

        }
    }
}
