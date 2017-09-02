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

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    public class HelpersTests
    {
        [Fact]
        public void Create_Default()
        {
            var jobs = Enumerable.Range(0, 100)
                  .Select(x => "test");
            var enumerable = jobs as IList<string> ?? jobs.ToList();
            Assert.Equal(2, enumerable.Partition(50).Count());
            Assert.Equal(10, enumerable.Partition(10).Count());

            jobs = Enumerable.Range(0, 87)
                 .Select(x => "test");

            var part = jobs.Partition(20);

            var i = 0;
            foreach (var p in part)
            {
                Assert.Equal(i == 4 ? 7 : 20, p.Count());
                i++;
            }
        }
    }
}
