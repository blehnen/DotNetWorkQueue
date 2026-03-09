using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class BaseMonitorTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateMonitor();
            Assert.IsFalse(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = CreateMonitor();
            test.Dispose();
            test.Dispose();
        }

        [TestMethod]
        public void Start_Runs_Action()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var action = Substitute.For<Func<CancellationToken, long>>();
            var monitor = Substitute.For<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(action, monitor, fixture.Create<ILogger>()))
            {
                test.Start();
                Thread.Sleep(3000);
            }
            Assert.ContainsSingle(action.ReceivedCalls());
        }

        [TestMethod]
        public void Start_Stop_Start_Works()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var action = Substitute.For<Func<CancellationToken, long>>();
            var monitor = Substitute.For<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(action, monitor, fixture.Create<ILogger>()))
            {
                test.Start();
                Thread.Sleep(2000);
                test.Stop();
                test.Start();
                Thread.Sleep(2000);
            }
            Assert.AreEqual(2, action.ReceivedCalls().Count());
        }

        [TestMethod]
        public void Start_Stop_Works()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            long Action(CancellationToken cancel)
            {
                Thread.Sleep(2000);
                return 0;
            }

            var monitor = fixture.Create<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(Action, monitor, fixture.Create<ILogger>()))
            {
                test.Start();
                Thread.Sleep(500);
                test.Stop();
            }
        }

        [TestMethod]
        public void Dispose_Running_Instance_Works()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            long Action(CancellationToken cancel)
            {
                Thread.Sleep(2000);
                return 0;
            }

            var monitor = fixture.Create<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(Action, monitor, fixture.Create<ILogger>()))
            {
                test.Start();
                Thread.Sleep(500);
            }
        }

        [TestMethod]
        public void Disposed_Instance_Get_Running_NoException()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.IsFalse(test.RunningPublic);
        }

        [TestMethod]
        public void Disposed_Instance_Set_Running_Exception()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
            delegate
            {
                test.RunningPublic = true;
            });
        }

        private BaseMonitorTest CreateMonitor(Func<CancellationToken, long> action, IMonitorTimespan monitorTimespan, ILogger log)
        {
            return new BaseMonitorTest(action, monitorTimespan, log);
        }

        private BaseMonitorTest CreateMonitor()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            long Func(CancellationToken cancel) => 0;
            return CreateMonitor(Func, fixture.Create<IMonitorTimespan>(), fixture.Create<ILogger>());
        }
    }

    internal class BaseMonitorTest : BaseMonitor
    {
        public BaseMonitorTest(Func<CancellationToken, long> monitorAction, IMonitorTimespan monitorTimeSpan, ILogger log)
            : base(monitorAction, monitorTimeSpan, log)
        {

        }

        public bool RunningPublic
        {
            get => Running;
            set => Running = value;
        }
    }
}
