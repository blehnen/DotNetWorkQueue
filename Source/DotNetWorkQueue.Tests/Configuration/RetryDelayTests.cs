using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Factory;



using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class RetryDelayTests
    {
        [Fact]
        public void DefaultCreation_IsEmpty()
        {
            var test  = GetConfiguration();
            Assert.Empty(test.RetryTypes);
        }

        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.False(configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.True(configuration.IsReadOnly);
        }
        [Fact]
        public void Add_Null_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<ArgumentNullException>(
              delegate
              {
                  configuration.Add(null, null);
              });
        }
        [Fact]
        public void Add_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                configuration.Add(typeof(Exception), new List<TimeSpan>());
              });
        }

        [Theory, AutoData]
        public void Add_OneException_OneTime(TimeSpan value)
        {
            var test = GetConfiguration();
            
            test.Add(typeof(NullReferenceException), new List<TimeSpan> { value });
            Assert.Equal(value, test.GetRetryAmount(new NullReferenceException()).Times[0]);
        }

        [Theory, AutoData]
        public void Add_TwoException_TwoTimes(TimeSpan value)
        {
            var test = GetConfiguration();

            test.Add(typeof(NullReferenceException), new List<TimeSpan> { value });
            test.Add(typeof(ArgumentException), new List<TimeSpan> { value, TimeSpan.MaxValue });

            Assert.Equal(TimeSpan.MaxValue, test.GetRetryAmount(new ArgumentException()).Times[1]);
        }

        [Fact]
        public void Get_MissingException_TimeList_Empty()
        {
            var test = GetConfiguration();
            Assert.Empty(test.GetRetryAmount(new ArgumentException()).Times);
        }

        [Theory, AutoData]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing(TimeSpan value)
        {
            var test = GetConfiguration();
            test.Add(typeof(Exception), new List<TimeSpan> { value });

            Assert.Equal(value, test.GetRetryAmount(new ArgumentException()).Times[0]);
        }

        [Theory, AutoData]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing_Multiple(TimeSpan value1, TimeSpan value2)
        {
            var test = GetConfiguration();
            test.Add(typeof(DotNetWorkQueueException), new List<TimeSpan> { value1 });
            test.Add(typeof(Exception), new List<TimeSpan> { value2 });

            Assert.Equal(value1, test.GetRetryAmount(new CommitException()).Times[0]);
        }

        [Theory, AutoData]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing_Multiple_Reverse(TimeSpan value)
        {
            var test = GetConfiguration();
            test.Add(typeof(Exception), new List<TimeSpan> { value });
            test.Add(typeof(DotNetWorkQueueException), new List<TimeSpan> { value });

            Assert.Equal(value, test.GetRetryAmount(new CommitException()).Times[0]);
        }

        [Fact]
        public void Add_DuplicateType_Fails()
        {
            var configuration = GetConfiguration();
            
            configuration.Add(typeof(Exception), new List<TimeSpan>());

            Assert.Throws<ArgumentException>(
              delegate
              {
                  configuration.Add(typeof(Exception), new List<TimeSpan>());
              });
        }

        private RetryDelay GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var configuration = fixture.Create<RetryInformationFactory>();
            fixture.Inject<IRetryInformationFactory>(configuration);
            return fixture.Create<RetryDelay>();
        }
    }
}
