using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.TaskScheduling;
using NSubstitute;
using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.TaskScheduling
{
    public class SchedulerTaskFactoryTests
    {
        [Fact]
        public void GetSet_Scheduler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var scheduler = fixture.Create<ATaskScheduler>();
            var test = Create(scheduler);
            Assert.Equal(scheduler, test.Scheduler);
        }

        [Fact]
        public void TryStartNew_Null_Action_Exception()
        {
            var test = Create();

            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    test.TryStartNew(null, new StateInformation(Substitute.For<IWorkGroup>()), x => { },
                        out _);
                });
        }

        [Fact]
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
