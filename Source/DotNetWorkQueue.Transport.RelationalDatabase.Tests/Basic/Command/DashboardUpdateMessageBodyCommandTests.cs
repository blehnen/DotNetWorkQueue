// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Command
{
    public class DashboardUpdateMessageBodyCommandTests
    {
        [Fact]
        public void Create_Default()
        {
            const long id = 7L;
            var body = new byte[] { 1, 2, 3 };
            var headers = new byte[] { 4, 5, 6 };

            var test = new DashboardUpdateMessageBodyCommand(id, body, headers);

            Assert.Equal(id, test.QueueId);
            Assert.Equal(body, test.Body);
            Assert.Equal(headers, test.Headers);
        }
    }
}
