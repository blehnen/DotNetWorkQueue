using DotNetWorkQueue.Trace;
using Xunit;
namespace DotNetWorkQueue.Tests.Trace
{
    public class DataMappingTextMapTests
    {
        [Fact()]
        public void Set_Test()
        {
            var map = new DataMappingTextMap();
            map.Set("test", "test1");
            Assert.True(map.ContainsKey("test"));
            map.Set("test1", "test2");
            Assert.True(map.ContainsKey("test1"));
            Assert.False(map.ContainsKey("test2"));
        }
    }
}