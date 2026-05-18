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
using System.Data.Common;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests
{
    /// <summary>
    /// Locks the public contract of <see cref="IRelationalWorkerNotification"/>: inheritance from
    /// <see cref="IWorkerNotification"/>, single read-only <see cref="DbTransaction"/> property,
    /// and public visibility. Failure of any assertion here indicates an accidental breaking change
    /// to the inbox-pattern capability surface.
    /// </summary>
    [TestClass]
    public class IRelationalWorkerNotificationContractTests
    {
        [TestMethod]
        public void Interface_Is_Public()
        {
            var type = typeof(IRelationalWorkerNotification);
            Assert.IsTrue(type.IsInterface, "IRelationalWorkerNotification must be an interface.");
            Assert.IsTrue(type.IsPublic, "IRelationalWorkerNotification must be public.");
        }

        [TestMethod]
        public void Interface_Inherits_IWorkerNotification()
        {
            var type = typeof(IRelationalWorkerNotification);
            Assert.IsTrue(
                typeof(IWorkerNotification).IsAssignableFrom(type),
                "IRelationalWorkerNotification must inherit IWorkerNotification.");
        }

        [TestMethod]
        public void Transaction_Property_Exists_With_Expected_Type()
        {
            var prop = typeof(IRelationalWorkerNotification)
                .GetProperty("Transaction", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(prop, "Transaction property must exist on IRelationalWorkerNotification.");
            Assert.AreEqual(
                typeof(DbTransaction),
                prop!.PropertyType,
                "Transaction property must be typed as System.Data.Common.DbTransaction (NOT System.Data.IDbTransaction).");
        }

        [TestMethod]
        public void Transaction_Property_Is_Read_Only()
        {
            var prop = typeof(IRelationalWorkerNotification)
                .GetProperty("Transaction", BindingFlags.Public | BindingFlags.Instance);

            Assert.IsNotNull(prop);
            Assert.IsTrue(prop!.CanRead, "Transaction must expose a getter.");
            Assert.IsNull(
                prop.GetSetMethod(nonPublic: false),
                "Transaction must NOT expose a public setter — the library owns the transaction.");
        }

        [TestMethod]
        public void Interface_Declares_Exactly_One_New_Property()
        {
            // DeclaredOnly excludes IWorkerNotification members; only the additive surface should appear.
            var declared = typeof(IRelationalWorkerNotification).GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.AreEqual(1, declared.Length,
                "IRelationalWorkerNotification must declare exactly one new property (Transaction).");
            Assert.AreEqual("Transaction", declared[0].Name);
        }
    }
}
