using System;
using System.Collections.Generic;
using DotNetWorkQueue.TaskScheduling;
using Xunit;

namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class WorkGroupTests
    {
        private const string Name = "test";

        [Fact]
        public void Create_Null_Constructor_Fails()
        {
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                var test = new WorkGroup(null, 0, 0);
                Assert.Null(test);
            });
        }
        [Fact]
        public void Create_Zero_Concurrency_Fails()
        {
            Assert.Throws<ArgumentException>(
            delegate
            {
                var test = new WorkGroup(Name, 0, 0);
                Assert.Null(test);
            });
        }

        [Fact]
        public void Create_Zero_Queue_Ok()
        {
           var test = new WorkGroup(Name, 1, 0);
           Assert.Equal(0, test.MaxQueueSize);
        }

        [Fact]
        public void GetSet_Name()
        {
            var test = new WorkGroup(Name, 1, 0);
            Assert.Equal(Name, test.Name);
        }

        [Fact]
        public void GetSet_Concurrency()
        {
            var test = new WorkGroup(Name, 1, 0);
            Assert.Equal(1, test.ConcurrencyLevel);
        }

        [Fact]
        public void GetSet_MaxQueueSize()
        {
            var test = new WorkGroup(Name, 1, 10);
            Assert.Equal(10, test.MaxQueueSize);
        }

        [Fact]
        public void Test_ToString()
        {
            var test = new WorkGroup(Name, 1, 10);
            Assert.Equal(Name, test.ToString());
        }

        [Fact]
        public void Test_Equals()
        {
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup(Name, 50, 50);
            Assert.True(test.Equals(test2));
        }

        [Fact]
        public void Test_NotEquals()
        {
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup("not-equal", 50, 50);
            Assert.False(test.Equals(test2));
        }

        [Fact]
        public void Test_Equals_Null_False()
        {
            var test = new WorkGroup(Name, 1, 10);
            Assert.False(test.Equals(null));
        }

        [Fact]
        public void Test_HashCode_Equals()
        {
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup(Name, 50, 50);
            Assert.Equal(test.GetHashCode(), test2.GetHashCode());
        }

        [Fact]
        public void Test_HashCode_NotEquals()
        {
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup("Another Name", 50, 50);
            Assert.NotEqual(test.GetHashCode(), test2.GetHashCode());
        }

        [Fact]
        public void Test_Hash_Duplicate_Fails()
        {
            // ReSharper disable once CollectionNeverQueried.Local
            var tests = new Dictionary<WorkGroup, WorkGroup>();
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup(Name, 50, 50);

            tests.Add(test, test);
            Assert.Throws<ArgumentException>(
           delegate
           {
               tests.Add(test2, test2);
           });
        }

        [Fact]
        public void Test_Hash()
        {
            var tests = new Dictionary<WorkGroup, WorkGroup>();
            var test = new WorkGroup(Name, 1, 10);
            var test2 = new WorkGroup("Another name", 50, 50);
   
            tests.Add(test, test);
            tests.Add(test2, test2);

            Assert.Equal(2, tests.Count);
            Assert.Equal(test, tests[test]);
            Assert.Equal(test2, tests[test2]);
            Assert.NotEqual(test, tests[test2]);
        }

        [Fact]
        public void Test_Hash_Multi()
        {
            var tests = new Dictionary<WorkGroup, WorkGroup>(100000);
            for (var i = 0; i < 100000; i++)
            {
                var test = new WorkGroup(string.Concat(Name, i.ToString()), i+1, i);
                tests.Add(test, test);
            }
            Assert.Equal(100000, tests.Count);
        }
    }
}
