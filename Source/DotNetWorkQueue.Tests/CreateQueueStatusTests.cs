// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using DotNetWorkQueue.Tests.IoC;
using Xunit;

namespace DotNetWorkQueue.Tests
{
    [Collection("IoC")]
    public class CreateQueueStatusTests
    {
        [Fact]
        public void Create_Null_Services_Fails()
        {
            using (var test = new QueueStatusContainer(null))
            {
                Assert.Throws<NullReferenceException>(
                    delegate
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        test.CreateStatus();
                    });
            }
        }
        [Fact]
        public void Create_Status()
        {
            using (var test = new QueueStatusContainer())
            {
                using (test.CreateStatus())
                {

                }
            }
        }

        [Fact]
        public void Create_Status_Provider()
        {
            using (var test = new QueueStatusContainer())
            {
                Assert.NotNull(test.CreateStatusProvider<CreateContainerTest.NoOpDuplexTransport>("queue", "connection"));
                Assert.NotNull(test.CreateStatusProvider<CreateContainerTest.NoOpDuplexTransport>(container => { },
                    "queue", "connection"));
            }
        }
    }
}
