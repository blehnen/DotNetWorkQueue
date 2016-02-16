// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerNotificationTests
    {
        [Fact]
        public void Rollback_Disabled_Default()
        {
            var test = Create();
            Assert.False(test.TransportSupportsRollback);
        }

        [Fact]
        public void Rollback_Enabled()
        {
            var test = Create(true);
            Assert.True(test.TransportSupportsRollback);
        }

        [Fact]
        public void WorkerStopping_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.WorkerStopping);
        }

        [Fact]
        public void HeaderNames_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.HeaderNames);
        }

        public void Log_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.Log);
        }

        private WorkerNotification Create(bool enableRollback = false)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<TransportConfigurationReceive>();
            configuration.MessageRollbackSupported = enableRollback;
            fixture.Inject(configuration);
            return fixture.Create<WorkerNotification>();
        }
    }
}
