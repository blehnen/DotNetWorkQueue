using System;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class ClearHistoryMonitorTests
    {
        private static readonly DateTime FixedNow = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        [TestMethod]
        public void PurgeCutoff_ComesFromTheConfiguredTimeProvider()
        {
            //the cut-off is compared against timestamps the transport wrote. Taken from this machine
            //instead, the retention window moves by the difference between the two clocks - so records
            //are kept too long, or deleted early
            const int retentionDays = 30;
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var historyOptions = Substitute.For<IHistoryTransportOptions>();
            historyOptions.RetentionDays.Returns(retentionDays);
            historyOptions.MonitorTime.Returns(TimeSpan.FromHours(1));
            var options = Substitute.For<IBaseTransportOptions>();
            options.HistoryOptions.Returns(historyOptions);

            var purge = Substitute.For<IPurgeMessageHistory>();

            using (var monitor = new ClearHistoryMonitor(options, purge, fixture.Create<ILogger>(), FixedClock()))
            {
                monitor.Start();
                Thread.Sleep(3000);
            }

            purge.Received(1).Purge(FixedNow.AddDays(-retentionDays));
        }

        private static IGetTimeFactory FixedClock()
        {
            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(FixedNow);
            var factory = Substitute.For<IGetTimeFactory>();
            factory.Create().Returns(time);
            return factory;
        }
    }
}
