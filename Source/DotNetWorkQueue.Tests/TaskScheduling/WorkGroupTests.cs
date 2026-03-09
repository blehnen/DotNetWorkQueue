using DotNetWorkQueue.TaskScheduling;
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class WorkGroupTests
    {
        private const string Name = "test";

        [TestMethod]
        public void Create_Null_Constructor_Fails()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
            delegate
            {
                var test = new WorkGroup(null, 0);
                Assert.IsNull(test);
            });
        }
        [TestMethod]
        public void Create_Zero_Concurrency_Fails()
        {
            Assert.ThrowsExactly<ArgumentException>(
            delegate
            {
                var test = new WorkGroup(Name, 0);
                Assert.IsNull(test);
            });
        }


        [TestMethod]
        public void GetSet_Name()
        {
            var test = new WorkGroup(Name, 1);
            Assert.AreEqual(Name, test.Name);
        }

        [TestMethod]
        public void GetSet_Concurrency()
        {
            var test = new WorkGroup(Name, 1);
            Assert.AreEqual(1, test.ConcurrencyLevel);
        }

        [TestMethod]
        public void Test_ToString()
        {
            var test = new WorkGroup(Name, 1);
            Assert.AreEqual(Name, test.ToString());
        }

        [TestMethod]
        public void Test_Equals()
        {
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup(Name, 50);
            Assert.IsTrue(test.Equals(test2));
        }

        [TestMethod]
        public void Test_NotEquals()
        {
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup("not-equal", 50);
            Assert.IsFalse(test.Equals(test2));
        }

        [TestMethod]
        public void Test_Equals_Null_False()
        {
            var test = new WorkGroup(Name, 1);
            Assert.IsFalse(test.Equals(null));
        }

        [TestMethod]
        public void Test_HashCode_Equals()
        {
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup(Name, 50);
            Assert.AreEqual(test.GetHashCode(), test2.GetHashCode());
        }

        [TestMethod]
        public void Test_HashCode_NotEquals()
        {
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup("Another Name", 50);
            Assert.AreNotEqual(test.GetHashCode(), test2.GetHashCode());
        }

        [TestMethod]
        public void Test_Hash_Duplicate_Fails()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var tests = new Dictionary<WorkGroup, WorkGroup>();
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup(Name, 50);

            tests.Add(test, test);
            Assert.ThrowsExactly<ArgumentException>(
           delegate
           {
               tests.Add(test2, test2);
           });
        }

        [TestMethod]
        public void Test_Hash()
        {
            var tests = new Dictionary<WorkGroup, WorkGroup>();
            var test = new WorkGroup(Name, 1);
            var test2 = new WorkGroup("Another name", 50);

            tests.Add(test, test);
            tests.Add(test2, test2);

            Assert.AreEqual(2, tests.Count);
            Assert.AreEqual(test, tests[test]);
            Assert.AreEqual(test2, tests[test2]);
            Assert.AreNotEqual(test, tests[test2]);
        }

        [TestMethod]
        public void Test_Hash_Multi()
        {
            var tests = new Dictionary<WorkGroup, WorkGroup>(100000);
            for (var i = 0; i < 100000; i++)
            {
                var test = new WorkGroup(string.Concat(Name, i.ToString()), i + 1);
                tests.Add(test, test);
            }
            Assert.AreEqual(100000, tests.Count);
        }
    }
}
