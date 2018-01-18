using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class QueueDelayFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var factory = Create();
            var test = factory.Create();
            Assert.NotNull(test);
        }
        [Fact]
        public void Create_Default_TimeSpans()
        {
            var factory = Create();
            var list = new List<TimeSpan>
            {
                TimeSpan.FromHours(1),
                TimeSpan.FromHours(2),
                TimeSpan.FromHours(3)
            };
            var test = factory.Create(list);
            var i = 0;
            foreach (var t in test)
            {
                Assert.Equal(t, TimeSpan.FromHours(i + 1));
                i++;
            }
        }
        public IQueueDelayFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueDelayFactory>();
        }
    }
}
