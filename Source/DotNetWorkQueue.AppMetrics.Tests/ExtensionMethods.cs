using App.Metrics;
using Xunit;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    public class ExtensionMethods
    {
        [Fact]
        public void GetMetrics()
        {
            var metrics = new DotNetWorkQueue.AppMetrics.Metrics("test");
            var data = metrics.GetCurrentMetrics();
            Assert.NotNull(data);

            var metrics2 = new DotNetWorkQueue.AppMetrics.Metrics("test2");
            var data2 = metrics2.GetCurrentMetrics();
            Assert.NotNull(data2);
        }
    }
    public static class Creator
    {
        public static App.Metrics.IMetrics Create()
        {
            var metrics = new MetricsBuilder()
                .Configuration.Configure(
                    options =>
                    {
                        options.Enabled = true;
                        options.ReportingEnabled = true;
                    })
                .Build();

            return metrics;
        }
    }
}
