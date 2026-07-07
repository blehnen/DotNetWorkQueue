using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    [TestClass]
    public class HelpersTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var jobs = Enumerable.Range(0, 100)
                  .Select(x => "test");
            var enumerable = jobs as IList<string> ?? jobs.ToList();
            Assert.HasCount(2, enumerable.Partition(50));
            Assert.HasCount(10, enumerable.Partition(10));

            jobs = Enumerable.Range(0, 87)
                 .Select(x => "test");

            var part = jobs.Partition(20);

            var i = 0;
            foreach (var p in part)
            {
                Assert.HasCount(i == 4 ? 7 : 20, p);
                i++;
            }
        }
    }
}
