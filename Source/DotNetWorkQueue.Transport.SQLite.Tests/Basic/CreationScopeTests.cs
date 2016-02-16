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
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;
using System;
namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class CreationScopeTests
    {
        [Fact]
        public void Create_CreationScope()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
            }
            Assert.True(disposable.WasDisposed);
        }

        [Fact]
        public void Create_DisposeTwiceIsOk()
        {
            var disposable = new CanBeDisposed();
            using (var test = new CreationScope())
            {
                test.AddScopedObject(disposable);
                test.Dispose();
            }
            Assert.True(disposable.WasDisposed);
        }
    }
    internal class CanBeDisposed : IDisposable
    {
        public bool WasDisposed { get; private set; }
        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}
