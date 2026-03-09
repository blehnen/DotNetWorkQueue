using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Factory
{
    [TestClass]
    public class UnixTimeFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var options = Helpers.CreateOptions();
            fixture.Inject(options);
            var test = fixture.Create<UnixTimeFactory>();
            test.Create();

            options.TimeServer = TimeLocations.LocalMachine;
            test.Create();

            options.TimeServer = TimeLocations.SntpServer;
            test.Create();

            options.TimeServer = TimeLocations.Custom;
            test.Create();

            options.TimeServer = (TimeLocations)99;
            Assert.ThrowsExactly<DotNetWorkQueueException>(
           delegate
           {
               test.Create();
           });
        }
    }
}
