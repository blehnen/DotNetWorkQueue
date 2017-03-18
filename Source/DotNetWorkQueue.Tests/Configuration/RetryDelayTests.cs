// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Factory;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Configuration
{
    public class RetryDelayTests
    {
        [Fact]
        public void DefaultCreation_IsEmpty()
        {
            var test  = GetConfiguration();
            Assert.Equal(0, test.RetryTypes.Count);
        }

        [Fact]
        public void Test_DefaultNotReadOnly()
        {
            var configuration = GetConfiguration();
            Assert.Equal(false, configuration.IsReadOnly);
        }
        [Fact]
        public void Set_Readonly()
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();
            Assert.Equal(true, configuration.IsReadOnly);
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
            Assert.Equal(0, test.GetRetryAmount(new ArgumentException()).Times.Count);
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
