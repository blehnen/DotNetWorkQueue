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
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class ClearExpiredMessagesRpcMonitorTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = Create();
            Assert.Equal(test.IsDisposed, false);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = Create();
            test.Dispose();
            test.Dispose();
        }

        [Fact]
        public void Start_Stop()
        {
            var test = Create();
            test.Start();
            test.Stop();
            test.Dispose();
        }

        private IClearExpiredMessagesRpcMonitor Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ClearExpiredMessagesRpcMonitor>();
        }
    }
}
