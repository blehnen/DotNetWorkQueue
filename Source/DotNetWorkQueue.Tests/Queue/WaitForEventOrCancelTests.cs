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
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class WaitForEventOrCancelTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Equal(test.IsDisposed, true);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Cancel_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Cancel();
                    });
            }
        }

        [Fact]
        public void Reset_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Reset();
                    });
            }
        }

        [Fact]
        public void Wait_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Wait();
                    });
            }
        }
        [Fact]
        public void Set_IfDisposed_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
                    delegate
                    {
                        test.Set();
                    });
            }
        }

        [Fact]
        public void Wait_Set()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
                test.Wait();
            }
        }

        [Fact]
        public void Wait_Cancel()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Cancel(); });
                test.Wait();
            }
        }

        [Fact]
        public void Wait_Set_Reset_Wait()
        {
            using (var test = Create())
            {
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
                test.Wait();
                test.Reset();
                Task.Factory.StartNew(() => { Thread.Sleep(1000); test.Set(); });
                test.Wait();
            }
        }

        private IWaitForEventOrCancel Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<WaitForEventOrCancel>();
        }
    }
}
