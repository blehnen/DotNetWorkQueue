using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;


using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class RetryInformationFactoryTests
    {
        [Fact]
        public void Create_Default()
        {
            var t = typeof (NullReferenceException);
            var times = new List<TimeSpan> {TimeSpan.MinValue, TimeSpan.MaxValue};

            var factory = Create();
            var info = factory.Create(t, times);

            Assert.Equal(info.ExceptionType, t);
            Assert.Equal(info.Times, times);
            Assert.Equal(info.MaxRetries, times.Count);
        }
        [Fact]
        public void Create_Null_Params_Fails()
        {
            var factory = Create();
            Assert.Throws<ArgumentNullException>(
               delegate
               {
                   factory.Create(null, null);
               });
        }
        private IRetryInformationFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RetryInformationFactory>();
        }
    }
}
