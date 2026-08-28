// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Validation
{
    /// <summary>
    /// The compiler-supplied argument names have to match what the expression-tree overloads
    /// produced, because callers - this library's own tests included - assert on
    /// <see cref="ArgumentException.ParamName"/>.
    /// </summary>
    [TestClass]
    public class GuardTests
    {
        [TestMethod]
        public void NotNull_Names_The_Argument_It_Was_Given()
        {
            object someArgument = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => Guard.NotNull(someArgument));

            Assert.AreEqual("someArgument", ex.ParamName);
        }

        [TestMethod]
        public void NotNull_Returns_The_Value_When_It_Is_Not_Null()
        {
            var someArgument = new object();

            Assert.AreSame(someArgument, Guard.NotNull(someArgument));
        }

        [TestMethod]
        public void NotNull_Accepts_A_Value_Type()
        {
            //T is unconstrained, so a value type boxes to a non-null reference and must pass.
            Assert.AreEqual(0, Guard.NotNull(0));
        }

        [TestMethod]
        public void NotNull_Honours_An_Explicit_Name()
        {
            object someArgument = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => Guard.NotNull(someArgument, "chosenName"));

            Assert.AreEqual("chosenName", ex.ParamName);
        }

        [TestMethod]
        public void NotNullOrEmpty_Throws_ArgumentNull_For_Null()
        {
            string someArgument = null;

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() => Guard.NotNullOrEmpty(someArgument));

            Assert.AreEqual("someArgument", ex.ParamName);
        }

        [TestMethod]
        public void NotNullOrEmpty_Throws_Argument_For_Empty()
        {
            var someArgument = string.Empty;

            var ex = Assert.ThrowsExactly<ArgumentException>(() => Guard.NotNullOrEmpty(someArgument));

            Assert.AreEqual("someArgument", ex.ParamName);
        }

        [TestMethod]
        public void NotNullOrEmpty_Passes_A_Non_Empty_String()
        {
            Guard.NotNullOrEmpty("a value");
        }

        [TestMethod]
        public void IsValid_Throws_With_The_Message_And_The_Argument_Name()
        {
            var someArgument = 0;

            var ex = Assert.ThrowsExactly<ArgumentException>(
                () => Guard.IsValid(someArgument, i => i > 0, "must be greater than 0"));

            Assert.AreEqual("someArgument", ex.ParamName);
            Assert.Contains("must be greater than 0", ex.Message);
        }

        [TestMethod]
        public void IsValid_Passes_A_Valid_Value()
        {
            Guard.IsValid(1, i => i > 0, "must be greater than 0");
        }
    }
}
