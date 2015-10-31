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

#region Using

using System;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    public class ColumnTests
    {
        [Fact]
        public void GetSet_Nullable()
        {
            var test = new Column();
            var c = test.Nullable;
            test.Nullable = !c;
            Assert.Equal(!c, test.Nullable);
        }
        [Fact]
        public void GetSet_Default()
        {
            var test = new Column();
            var d = new Default("", "");
            test.Default = d;
            Assert.Equal(d, test.Default);
        }
        [Fact]
        public void GetSet_Identity()
        {
            var test = new Column();
            var d = new Identity(1, 1);
            test.Identity = d;
            Assert.Equal(d, test.Identity);
        }
        [Fact]
        public void GetSet_Length()
        {
            var test = new Column();
            var c = test.Length;
            test.Length = c + 1;
            Assert.Equal(c + 1, test.Length);
        }
        [Fact]
        public void GetSet_Position()
        {
            var test = new Column();
            var c = test.Position;
            test.Position = c + 1;
            Assert.Equal(c + 1, test.Position);
        }
        [Fact]
        public void GetSet_Precision()
        {
            var b = new byte();
            var test = new Column {Precision = b};
            Assert.Equal(b, test.Precision);
        }
        [Fact]
        public void GetSet_Scale()
        {
            var test = new Column();
            var c = test.Scale;
            test.Scale = c + 1;
            Assert.Equal(c + 1, test.Scale);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Column {Name = "test"};
            Assert.Equal("test", test.Name);
        }
        [Fact]
        public void GetSet_Types()
        {
            var test = new Column();
            foreach (ColumnTypes type in Enum.GetValues(typeof(ColumnTypes)))
            {
                test.Type = type;
                Assert.Equal(type, test.Type);
            }
        }

        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Column>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
