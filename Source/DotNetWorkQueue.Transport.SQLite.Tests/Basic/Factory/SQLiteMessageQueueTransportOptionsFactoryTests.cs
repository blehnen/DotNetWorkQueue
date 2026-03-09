using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic.Factory
{
    [TestClass]
    public class SqLiteMessageQueueTransportOptionsFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<SqLiteMessageQueueTransportOptionsFactory>();
            test.Create();
        }
    }
}
