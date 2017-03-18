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

using System.Collections.Generic;
using DotNetWorkQueue.Transport.SQLite.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    public class ConstraintTests
    {
        [Fact]
        public void GetSet_Unique()
        {
            var test = new Constraint("test", ContraintType.Constraint, new List<string>());
            var c = test.Unique;
            test.Unique = !c;
            Assert.Equal(!c, test.Unique);
        }
        [Fact]
        public void GetSet_Columns()
        {
            var columns = new List<string> {"test"};
            var test = new Constraint("test", ContraintType.Constraint, columns);
            Assert.Equal(columns, test.Columns);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Constraint("test", ContraintType.Constraint, new List<string>()) {Name = "test1"};
            Assert.Equal("test1", test.Name);
        }
        [Fact]
        public void GetSet_ContraintType()
        {
            var test = new Constraint("test", ContraintType.Constraint, new List<string>());
            Assert.Equal(ContraintType.Constraint, test.Type);
        }

        [Fact]
        public void Script()
        {
            var test = new Constraint("test", ContraintType.PrimaryKey, new List<string>());
            Assert.Contains("PRIMARY KEY", test.Script());
        }

        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Constraint>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var config = new ComparisonConfig();
            config.MembersToIgnore.Add("Table"); //table should never be cloned
            var compareLogic = new CompareLogic(config);
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
