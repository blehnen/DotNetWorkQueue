using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    [TestClass]
    public class GetDashboardStaleMessagesPrepareHandlerTests
    {
        [TestMethod]
        public void Handle_Sets_CommandText()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(60, 0, 25), command, CommandStringTypes.GetDashboardStaleMessages);

            Assert.IsNotNull(command.CommandText);
        }

        [TestMethod]
        public void Handle_Adds_Three_Parameters()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(120, 2, 50), command, CommandStringTypes.GetDashboardStaleMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            Assert.AreEqual(4, parameters.Count);
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Threshold"));
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@ThresholdTicks"));
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@Offset"));
            Assert.IsTrue(parameters.Any(p => p.ParameterName == "@PageSize"));
        }

        [TestMethod]
        public void Handle_Computes_Offset_From_PageIndex()
        {
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(60, 3, 10), command, CommandStringTypes.GetDashboardStaleMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            var offset = parameters.First(p => p.ParameterName == "@Offset");
            Assert.AreEqual(30, offset.Value); // 3 * 10
        }

        private static GetDashboardStaleMessagesPrepareHandler CreateHandler()
        {
            var cache = new FakeCommandStringCache();
            var optionsFactory = Substitute.For<ITransportOptionsFactory>();
            var options = Substitute.For<ITransportOptions>();
            optionsFactory.Create().Returns(options);
            return new GetDashboardStaleMessagesPrepareHandler(cache, optionsFactory, FixedClock());
        }

        /// <summary>A date far from any machine clock, so a threshold from the wrong source cannot match.</summary>
        private static readonly DateTime ProviderNow = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        private static IGetTimeFactory FixedClock()
        {
            var time = Substitute.For<IGetTime>();
            time.GetCurrentUtcDate().Returns(ProviderNow);
            var factory = Substitute.For<IGetTimeFactory>();
            factory.Create().Returns(time);
            return factory;
        }

        [TestMethod]
        public void Handle_TakesTheThresholdFromTheConfiguredTimeProvider()
        {
            //staleness is measured against heartbeats the transport wrote, so the cut-off has to be on
            //the same clock as those - not on whichever machine happens to be asking
            var handler = CreateHandler();
            var command = CreateDbCommand();

            handler.Handle(new GetDashboardStaleMessagesQuery(120, 0, 50), command, CommandStringTypes.GetDashboardStaleMessages);

            var parameters = (DataParameterCollection)command.Parameters;
            var ticks = parameters.First(p => p.ParameterName == "@ThresholdTicks");
            Assert.AreEqual(ProviderNow.AddSeconds(-120).Ticks, ticks.Value);
        }

        private static DbCommand CreateDbCommand()
        {
            var command = Substitute.For<DbCommand>();
            var parameters = new DataParameterCollection();
            command.Parameters.Returns(parameters);
            command.CreateParameter().Returns(_ => Substitute.For<DbParameter>());
            return command;
        }
    }
}
