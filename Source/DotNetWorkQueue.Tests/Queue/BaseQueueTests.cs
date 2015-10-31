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
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class BaseQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            var test = CreateQueue();
            Assert.Equal(test.IsDisposed, false);
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [Fact]
        public void Disposed_Instance_Set_ShouldWork_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.ShouldWorkPublic = true;
            });
        }

        [Fact]
        public void Disposed_Instance_Get_ShouldWork_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Equal(false, test.ShouldWorkPublic);
        }

        public void Disposed_Instance_Set_Started_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.StartedPublic = true;
            });
        }

        [Fact]
        public void Disposed_Instance_Get_Started_NoException()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Equal(false, test.StartedPublic);
        }

        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var test = CreateQueue();
            test.Dispose();
            test.Dispose();
        }

        [Fact]
        public void SetGet_Started()
        {
            var test = CreateQueue();
            Assert.Equal(false, test.StartedPublic);
            test.StartedPublic = true;
            Assert.Equal(true, test.StartedPublic);
        }

        [Fact]
        public void SetGet_ShouldWork()
        {
            var test = CreateQueue();
            Assert.Equal(false, test.ShouldWorkPublic);
            test.ShouldWorkPublic = true;
            Assert.Equal(true, test.ShouldWorkPublic);
        }

        private BaseQueueTest CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return new BaseQueueTest(fixture.Create<ILogFactory>());
        }
    }

    public class BaseQueueTest : BaseQueue
    {
        public BaseQueueTest(ILogFactory log): base(log)
        {
            
        }

        public bool ShouldWorkPublic
        {
            get { return ShouldWork;}
            set { ShouldWork = value; }
        }
        public bool StartedPublic
        {
            get { return Started; }
            set { Started = value; }
        }
    }
}
