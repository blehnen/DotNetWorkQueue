using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests
{
    public class HelpersTests
    {
        [Fact]
        public void Create_Default()
        {
            var jobs = Enumerable.Range(0, 100)
                  .Select(x => "test");
            var enumerable = jobs as IList<string> ?? jobs.ToList();
            Assert.Equal(2, enumerable.Partition(50).Count());
            Assert.Equal(10, enumerable.Partition(10).Count());

            jobs = Enumerable.Range(0, 87)
                 .Select(x => "test");

            var part = jobs.Partition(20);

            var i = 0;
            foreach (var p in part)
            {
                Assert.Equal(i == 4 ? 7 : 20, p.Count());
                i++;
            }
        }
    }
}
