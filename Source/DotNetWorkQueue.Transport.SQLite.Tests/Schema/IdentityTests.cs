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
#region Using

using DotNetWorkQueue.Transport.SQLite.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    public class IdentityTests
    {
        [Fact]
        public void Script()
        {
            var test = new Identity();
            Assert.Contains("PRIMARY KEY AUTOINCREMENT", test.Script());
        }
        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Identity>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
