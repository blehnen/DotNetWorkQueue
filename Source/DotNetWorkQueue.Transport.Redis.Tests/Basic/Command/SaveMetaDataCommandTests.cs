using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    public class SaveMetaDataCommandTests
    {
        [Theory, AutoData]
        public void Create_Default(string number, int metaNumber)
        {
            var id = new RedisQueueId(number);
            var meta = new RedisMetaData(metaNumber);
            var test = new SaveMetaDataCommand(id, meta);
            Assert.Equal(id, test.Id);
            Assert.Equal(meta, test.MetaData);
        }
    }
}
