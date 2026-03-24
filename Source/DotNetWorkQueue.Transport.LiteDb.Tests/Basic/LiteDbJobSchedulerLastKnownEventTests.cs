using System;
using DotNetWorkQueue.Transport.LiteDb.Basic;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.LiteDb.Tests.Basic
{
    [TestClass]
    public class LiteDbJobSchedulerLastKnownEventTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var handler = Substitute.For<IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            Assert.IsNotNull(new LiteDbJobSchedulerLastKnownEvent(handler));
        }

        [TestMethod]
        public void Get_Returns_QueryHandler_Result()
        {
            var expected = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
            var handler = Substitute.For<IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            handler.Handle(Arg.Any<GetJobLastKnownEventQuery>()).Returns(expected);

            var scheduler = new LiteDbJobSchedulerLastKnownEvent(handler);
            var result = scheduler.Get("testJob");

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Get_Passes_JobName_To_Query()
        {
            var handler = Substitute.For<IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            handler.Handle(Arg.Any<GetJobLastKnownEventQuery>()).Returns(DateTimeOffset.MinValue);

            var scheduler = new LiteDbJobSchedulerLastKnownEvent(handler);
            scheduler.Get("mySpecificJob");

            handler.Received(1).Handle(Arg.Is<GetJobLastKnownEventQuery>(q => q.JobName == "mySpecificJob"));
        }

        [TestMethod]
        public void Get_Returns_Default_When_No_Event()
        {
            var handler = Substitute.For<IQueryHandler<GetJobLastKnownEventQuery, DateTimeOffset>>();
            handler.Handle(Arg.Any<GetJobLastKnownEventQuery>()).Returns(default(DateTimeOffset));

            var scheduler = new LiteDbJobSchedulerLastKnownEvent(handler);
            var result = scheduler.Get("nonExistentJob");

            Assert.AreEqual(default(DateTimeOffset), result);
        }
    }
}
