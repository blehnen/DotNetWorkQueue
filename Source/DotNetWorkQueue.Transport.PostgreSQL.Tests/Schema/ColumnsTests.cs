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
#region Using

using System.Linq;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
{
    public class ColumnsTests
    {
        [Fact]
        public void Add_Column()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            Assert.True(test.Items.Any(item => item.Name == "testing"));
        }
        [Fact]
        public void Remove_Column()
        {
            var test = new Columns();
            var column = new Column("testing", ColumnTypes.Bigint, true);
            test.Add(column);
            test.Remove(column);
            Assert.False(test.Items.Any(item => item.Name == "testing"));
        }
        [Fact]
        public void Script()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            Assert.Contains("testing", test.Script());
        }
    }
}
