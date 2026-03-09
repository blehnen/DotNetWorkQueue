using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Command
{
    [TestClass]
    public class SaveMetaDataCommandTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var metaNumber = fixture.Create<int>();
            var id = new RedisQueueId(number);
            var meta = new RedisMetaData(metaNumber);
            var test = new SaveMetaDataCommand(id, meta);
            Assert.AreEqual(id, test.Id);
            Assert.AreEqual(meta, test.MetaData);
        }
    }
}
