using Xunit;

namespace DotNetWorkQueue.Metrics.Net.Tests
{
    public class ExtensionMethods
    {
        [Fact]
        public void GetMetrics()
        {
            IMetrics metrics = new Net.Metrics("test");
            var data = metrics.GetCurrentMetrics();
            Assert.NotNull(data);

            var metrics2 = new Net.Metrics("test2");
            var data2 = metrics2.GetCurrentMetrics();
            Assert.NotNull(data2);
        }
    }
}
