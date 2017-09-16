// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class BaseMonitorTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateMonitor();
            Assert.False(test.IsDisposed);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = CreateMonitor();
            test.Dispose();
            test.Dispose();
        }

        [Fact]
        public void Start_Runs_Action()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var action = Substitute.For<Func<CancellationToken, long>>();
            var monitor = Substitute.For<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(action, monitor, fixture.Create<ILogFactory>()))
            {
                test.Start();
                Thread.Sleep(2000);
            }
            Assert.Equal(1, action.ReceivedCalls().Count());
        }

        [Fact]
        public void Start_Stop_Start_Works()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var action = Substitute.For<Func<CancellationToken, long>>();
            var monitor = Substitute.For<IMonitorTimespan>();
            monitor.MonitorTime.Returns(TimeSpan.FromHours(1));
            using (var test = CreateMonitor(action, monitor, fixture.Create<ILogFactory>()))
            {
                test.Start();
                Thread.Sleep(2000);
                test.Stop();
                test.Start();
                Thread.Sleep(2000);
            }
            Assert.Equal(2, action.ReceivedCalls().Count());
        }

        [Fact]
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
            using (var test = CreateMonitor(Action, monitor, fixture.Create<ILogFactory>()))
            {
                test.Start();
                Thread.Sleep(500);
                test.Stop();
            }
        }

        [Fact]
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
            using (var test = CreateMonitor(Action, monitor, fixture.Create<ILogFactory>()))
            {
                test.Start();
                Thread.Sleep(500);
            }
        }

        [Fact]
        public void Disposed_Instance_Get_Running_NoException()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.Equal(false, test.RunningPublic);
        }

        [Fact]
        public void Disposed_Instance_Set_Running_Exception()
        {
            var test = CreateMonitor();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.RunningPublic = true;
            });
        }

        private BaseMonitorTest CreateMonitor(Func<CancellationToken, long> action, IMonitorTimespan monitorTimespan, ILogFactory log)
        {
            return new BaseMonitorTest(action, monitorTimespan, log);
        }

        private BaseMonitorTest CreateMonitor()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            long Func(CancellationToken cancel) => 0;
            return CreateMonitor(Func, fixture.Create<IMonitorTimespan>(), fixture.Create<ILogFactory>());
        }
    }
    public class BaseMonitorTest : BaseMonitor
    {
        public BaseMonitorTest(Func<CancellationToken, long> monitorAction, IMonitorTimespan monitorTimeSpan, ILogFactory log)
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
