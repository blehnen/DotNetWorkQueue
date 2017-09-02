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
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Time
{
    public class SntpTimeConfigurationTests
    {
        [Fact]
        public void Create()
        {
            var test = new SntpTimeConfiguration();
            Assert.Equal(TimeSpan.FromSeconds(900), test.RefreshTime);
            Assert.Equal(123, test.Port);
            Assert.Equal("pool.ntp.org", test.Server);

            test.RefreshTime = TimeSpan.FromSeconds(100);
            Assert.Equal(TimeSpan.FromSeconds(100), test.RefreshTime);

            test.Port = 567;
            Assert.Equal(567, test.Port);

            test.Server = "test";
            Assert.Equal("test", test.Server);
        }
    }
}
