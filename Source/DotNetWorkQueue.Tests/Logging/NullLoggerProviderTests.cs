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
using System;
using DotNetWorkQueue.Logging;
using Xunit;
namespace DotNetWorkQueue.Tests.Logging
{
    public class NullLoggerProviderTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            var log = test.GetLogger("Test");
            Assert.Equal(true, log.Invoke(LogLevel.Debug, () => string.Empty));
            Assert.Equal(true, log.Invoke(LogLevel.Debug, () => string.Empty, new Exception("test")));
            Assert.Equal(true, log.Invoke(LogLevel.Debug, () => string.Empty, new Exception("test"), string.Empty));
        }
        [Fact]
        public void Create_OpenNestedContext()
        {
            var test = Create();
            using (test.OpenNestedContext("test"))
            {

            }

        }
        [Fact]
        public void Create_OpenMappedContext()
        {
            var test = Create();
            using (test.OpenMappedContext("test", "test"))
            {

            }
        }
        private NullLoggerProvider Create()
        {
            return new NullLoggerProvider();
        }
    }
}
