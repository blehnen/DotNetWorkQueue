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
using System.IO;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
{
    /// <summary>
    /// The key decides which sends and de-queues wait on each other. Too coarse and unrelated
    /// queues serialize; too fine and two connections to the same file stop excluding each other,
    /// which on the receive path means two consumers claiming the same message.
    /// </summary>
    [TestClass]
    public class LiteDbDatabaseKeyTests
    {
        [TestMethod]
        public void The_Same_File_Produces_The_Same_Key_Whatever_The_Connection_String_Looks_Like()
        {
            var file = Path.Combine(Path.GetTempPath(), "dnwq-key-test.db");

            var direct = KeyFor($"Filename={file};Connection=direct;");
            var shared = KeyFor($"Filename={file};Connection=shared;");
            var relative = KeyFor($"Filename={Path.Combine(Path.GetTempPath(), ".", "dnwq-key-test.db")};");

            Assert.AreEqual(direct, shared, "connection mode does not change which file is being written");
            Assert.AreEqual(direct, relative, "the same file reached by a different path is still the same file");
        }

        [TestMethod]
        public void Different_Files_Produce_Different_Keys()
        {
            var a = KeyFor($"Filename={Path.Combine(Path.GetTempPath(), "dnwq-key-a.db")};");
            var b = KeyFor($"Filename={Path.Combine(Path.GetTempPath(), "dnwq-key-b.db")};");

            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void An_In_Memory_Database_Is_Keyed_By_Its_Connection_String()
        {
            //an in-memory database belongs to the connection that opened it, so there is nothing
            //on disk to key on
            Assert.AreEqual(":memory:", KeyFor(":memory:"));
        }

        private static string KeyFor(string connectionString)
        {
            var scope = Substitute.For<ICreationScope>();
            using var manager = new LiteDbConnectionManager(
                new LiteDbConnectionInformation(new QueueConnection("aQueue", connectionString)), scope);
            return manager.DatabaseKey;
        }
    }

    [TestClass]
    public class LiteDbDatabaseLocksTests
    {
        [TestMethod]
        public void The_Same_Database_Gets_The_Same_Lock()
        {
            //this is the property the receive path depends on for correctness
            Assert.AreSame(DatabaseLocks.ForDequeue("KEY-A"), DatabaseLocks.ForDequeue("KEY-A"));
            Assert.AreSame(DatabaseLocks.ForJobs("KEY-A"), DatabaseLocks.ForJobs("KEY-A"));
        }

        [TestMethod]
        public void Different_Databases_Get_Different_Locks()
        {
            //this is the property that stops unrelated queues serializing against each other
            Assert.AreNotSame(DatabaseLocks.ForDequeue("KEY-B"), DatabaseLocks.ForDequeue("KEY-C"));
            Assert.AreNotSame(DatabaseLocks.ForJobs("KEY-B"), DatabaseLocks.ForJobs("KEY-C"));
        }

        [TestMethod]
        public void Queuing_A_Job_Does_Not_Wait_On_A_De_Queue()
        {
            //the two guard unrelated things; sharing one lock would reintroduce contention that
            //has no reason to exist
            Assert.AreNotSame(DatabaseLocks.ForJobs("KEY-D"), DatabaseLocks.ForDequeue("KEY-D"));
        }
    }
}
