using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    [TestClass]
    public class RedisQueueWorkSubFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<RedisQueueWorkSubFactory>();
            test.Create();
        }
    }
}
