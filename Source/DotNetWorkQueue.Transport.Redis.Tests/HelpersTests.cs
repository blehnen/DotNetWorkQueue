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
            Assert.AreEqual(2, enumerable.Partition(50).Count());
            Assert.AreEqual(10, enumerable.Partition(10).Count());

            jobs = Enumerable.Range(0, 87)
                 .Select(x => "test");

            var part = jobs.Partition(20);

            var i = 0;
            foreach (var p in part)
            {
                Assert.AreEqual(i == 4 ? 7 : 20, p.Count());
                i++;
            }
        }
    }
}
