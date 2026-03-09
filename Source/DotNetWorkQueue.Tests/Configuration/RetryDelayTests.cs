using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Factory;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class RetryDelayTests
    {
        [TestMethod]
        public void DefaultCreation_IsEmpty()
        {
            var test = GetConfiguration();
            Assert.IsEmpty(test.RetryTypes);
        }

        [TestMethod]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.IsFalse(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.IsTrue(configuration.IsReadOnly);
        }
        [TestMethod]
        public void Add_Null_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<ArgumentNullException>(
              delegate
              {
                  configuration.Add(null, null);
              });
        }
        [TestMethod]
        public void Add_WhenReadOnly_Fails()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.Add(typeof(Exception), new List<TimeSpan>());
              });
        }

        [TestMethod]
        public void Add_OneException_OneTime()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var test = GetConfiguration();

            test.Add(typeof(NullReferenceException), new List<TimeSpan> { value });
            Assert.AreEqual(value, test.GetRetryAmount(new NullReferenceException()).Times[0]);
        }

        [TestMethod]
        public void Add_TwoException_TwoTimes()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var test = GetConfiguration();

            test.Add(typeof(NullReferenceException), new List<TimeSpan> { value });
            test.Add(typeof(ArgumentException), new List<TimeSpan> { value, TimeSpan.MaxValue });

            Assert.AreEqual(TimeSpan.MaxValue, test.GetRetryAmount(new ArgumentException()).Times[1]);
        }

        [TestMethod]
        public void Get_MissingException_TimeList_Empty()
        {
            var test = GetConfiguration();
            Assert.IsEmpty(test.GetRetryAmount(new ArgumentException()).Times);
        }

        [TestMethod]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var test = GetConfiguration();
            test.Add(typeof(Exception), new List<TimeSpan> { value });

            Assert.AreEqual(value, test.GetRetryAmount(new ArgumentException()).Times[0]);
        }

        [TestMethod]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing_Multiple()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value1 = fixture.Create<TimeSpan>();
            var value2 = fixture.Create<TimeSpan>();
            var test = GetConfiguration();
            test.Add(typeof(DotNetWorkQueueException), new List<TimeSpan> { value1 });
            test.Add(typeof(Exception), new List<TimeSpan> { value2 });

            Assert.AreEqual(value1, test.GetRetryAmount(new CommitException()).Times[0]);
        }

        [TestMethod]
        public void Get_BaseExceptionReturned_IfExplicitException_Missing_Multiple_Reverse()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var test = GetConfiguration();
            test.Add(typeof(Exception), new List<TimeSpan> { value });
            test.Add(typeof(DotNetWorkQueueException), new List<TimeSpan> { value });

            Assert.AreEqual(value, test.GetRetryAmount(new CommitException()).Times[0]);
        }

        [TestMethod]
        public void Add_DuplicateType_Fails()
        {
            var configuration = GetConfiguration();

            configuration.Add(typeof(Exception), new List<TimeSpan>());

            Assert.ThrowsExactly<ArgumentException>(
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
