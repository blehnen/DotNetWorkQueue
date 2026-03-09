using App.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.AppMetrics.Tests
{
    [TestClass]
    public class ExtensionMethods
    {
        [TestMethod]
        public void GetMetrics()
        {
            var metrics = new DotNetWorkQueue.AppMetrics.Metrics("test");
            var data = metrics.GetCurrentMetrics();
            Assert.IsNotNull(data);

            var metrics2 = new DotNetWorkQueue.AppMetrics.Metrics("test2");
            var data2 = metrics2.GetCurrentMetrics();
            Assert.IsNotNull(data2);
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
