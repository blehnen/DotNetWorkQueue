using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    [TestClass]
    public class SchedulerTaskFactoryTests
    {
        [TestMethod]
        public void GetSet_Scheduler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var scheduler = fixture.Create<ATaskScheduler>();
            var test = Create(scheduler);
            Assert.AreEqual(scheduler, test.Scheduler);
        }

        [TestMethod]
        public void TryStartNew_Null_Action_Exception()
        {
            var test = Create();

            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    test.TryStartNew(null, new StateInformation(Substitute.For<IWorkGroup>()), x => { },
                        out _);
                });
        }

        [TestMethod]
        public void TryStartNew()
        {
            var test = Create();
            test.TryStartNew(x => { }, new StateInformation(Substitute.For<IWorkGroup>()), x => { },
                out _);
        }

        private SchedulerTaskFactory Create(ATaskScheduler scheduler)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(scheduler);
            var factory = fixture.Create<ITaskSchedulerFactory>();
            factory.Create().Returns(scheduler);
            fixture.Inject(factory);
            return fixture.Create<SchedulerTaskFactory>();
        }

        private SchedulerTaskFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var scheduler = fixture.Create<ATaskScheduler>();
            return Create(scheduler);
        }
    }
}
