using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Configuration;



using Xunit;

namespace DotNetWorkQueue.Tests.Configuration
{
    public class QueueDelayTests
    {
        [Fact]
        public void Test_Enumerator2()
        {
            var test = GetConfiguration();
            test.Add(TimeSpan.MaxValue);
            Assert.True(test.Count() == 1);
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
        [Theory, AutoData]
        public void Set_Add_WhenReadOnly_Fails(TimeSpan value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.Add(value);
              });
        }
        [Theory, AutoData]
        public void Set_AddRange_WhenReadOnly_Fails(List<TimeSpan> value)
        {
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.Throws<InvalidOperationException>(
              delegate
              {
                  configuration.Add(value);
              });
        }
        [Theory, AutoData]
        public void Set_AndReadOne(TimeSpan value)
        {
            var configuration = GetConfiguration();
            var temp = value;
            configuration.Add(temp);
            foreach (var t in configuration)
            {
                Assert.Equal(temp, t);
            }
        }
        [Theory, AutoData]
        public void Set_AndReadMultiple(TimeSpan value1, TimeSpan value2, TimeSpan value3)
        {
            var configuration = GetConfiguration();
            configuration.Add(value1);
            configuration.Add(value2);
            configuration.Add(value3);

            var i = 0;
            foreach (var t in configuration)
            {
                switch (i)
                {
                    case 0:
                        Assert.Equal(value1, t);
                        break;
                    case 1:
                        Assert.Equal(value2, t);
                        break;
                    case 2:
                        Assert.Equal(value3, t);
                        break;
                }
                i++;
            }
        }
        [Theory, AutoData]
        public void Set_AndReadList(TimeSpan value1, TimeSpan value2, TimeSpan value3)
        {
            var configuration = GetConfiguration();
            var list = new List<TimeSpan>(3) {value1, value2, value3};
            configuration.Add(list);
            var i = 0;
            foreach (var t in configuration)
            {
                switch (i)
                {
                    case 0:
                        Assert.Equal(value1, t);
                        break;
                    case 1:
                        Assert.Equal(value2, t);
                        break;
                    case 2:
                        Assert.Equal(value3, t);
                        break;
                }
                i++;
            }
        }
        [Theory, AutoData]
        public void Set_AndReadListCombo(TimeSpan value1, TimeSpan value2, TimeSpan value3, TimeSpan value4)
        {
            var configuration = GetConfiguration();
            var list = new List<TimeSpan>(3) { value1, value2, value3 };
            configuration.Add(list);
            configuration.Add(value4);

            var i = 0;
            foreach (var t in configuration)
            {
                switch (i)
                {
                    case 0:
                        Assert.Equal(value1, t);
                        break;
                    case 1:
                        Assert.Equal(value2, t);
                        break;
                    case 2:
                        Assert.Equal(value3, t);
                        break;
                    case 3:
                        Assert.Equal(value4, t);
                        break;
                }
                i++;
            }
        }
        private QueueDelay GetConfiguration()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueDelay>();
        }
    }
}
