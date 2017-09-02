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
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class WorkerHeartBeatNotificationTests
    {
        [Fact]
        public void ErrorCount_Zero()
        {
            var test = Create();
            Assert.Equal(0, test.ErrorCount);
        }

        [Fact]
        public void Set_Error_SetsException()
        {
            var test = Create();
            var error = new Exception();
            test.SetError(error);
            Assert.NotNull(test.Error);
            Assert.Equal(1, test.ErrorCount);
            Assert.Equal(error, test.Error);
        }

        [Fact]
        public void Status_SetGet()
        {
            var test = Create();
            var status = Substitute.For<IHeartBeatStatus>();
            test.Status = status;
            Assert.Equal(status, test.Status);
        }

        [Fact]
        public void ExceptionHasOccured_Default_NotNull()
        {
            var test = Create();
            Assert.NotNull(test.ExceptionHasOccured);
        }

        private IWorkerHeartBeatNotification Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WorkerHeartBeatNotification>();
        }
    }
}
