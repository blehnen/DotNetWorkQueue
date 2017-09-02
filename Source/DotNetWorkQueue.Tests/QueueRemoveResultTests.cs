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

using Xunit;

namespace DotNetWorkQueue.Tests
{
    public class QueueRemoveResultTests
    {
        [Fact]
        public void Create_SetStatus()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.Equal(QueueRemoveStatus.DoesNotExist, test.Status);
        }

        [Fact]
        public void Create_Success()
        {
            var test = new QueueRemoveResult(QueueRemoveStatus.DoesNotExist);
            Assert.False(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.Success);
            Assert.True(test.Success);

            test = new QueueRemoveResult(QueueRemoveStatus.None);
            Assert.False(test.Success);
        }
    }
}
