using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;



using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Configuration
{
    [TestClass]
    public class QueueDelayTests
    {
        [TestMethod]
        public void Test_Enumerator2()
        {
            var test = GetConfiguration();
            test.Add(TimeSpan.MaxValue);
            Assert.IsTrue(test.Count() == 1);
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
        public void Set_Add_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.Add(value);
              });
        }
        [TestMethod]
        public void Set_AddRange_WhenReadOnly_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<List<TimeSpan>>();
            var configuration = GetConfiguration();
            configuration.SetReadOnly();

            Assert.ThrowsExactly<InvalidOperationException>(
              delegate
              {
                  configuration.Add(value);
              });
        }
        [TestMethod]
        public void Set_AndReadOne()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            var temp = value;
            configuration.Add(temp);
            foreach (var t in configuration)
            {
                Assert.AreEqual(temp, t);
            }
        }
        [TestMethod]
        public void Set_AndReadMultiple()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value1 = fixture.Create<TimeSpan>();
            var value2 = fixture.Create<TimeSpan>();
            var value3 = fixture.Create<TimeSpan>();
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
                        Assert.AreEqual(value1, t);
                        break;
                    case 1:
                        Assert.AreEqual(value2, t);
                        break;
                    case 2:
                        Assert.AreEqual(value3, t);
                        break;
                }
                i++;
            }
        }
        [TestMethod]
        public void Set_AndReadList()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value1 = fixture.Create<TimeSpan>();
            var value2 = fixture.Create<TimeSpan>();
            var value3 = fixture.Create<TimeSpan>();
            var configuration = GetConfiguration();
            var list = new List<TimeSpan>(3) { value1, value2, value3 };
            configuration.Add(list);
            var i = 0;
            foreach (var t in configuration)
            {
                switch (i)
                {
                    case 0:
                        Assert.AreEqual(value1, t);
                        break;
                    case 1:
                        Assert.AreEqual(value2, t);
                        break;
                    case 2:
                        Assert.AreEqual(value3, t);
                        break;
                }
                i++;
            }
        }
        [TestMethod]
        public void Set_AndReadListCombo()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value1 = fixture.Create<TimeSpan>();
            var value2 = fixture.Create<TimeSpan>();
            var value3 = fixture.Create<TimeSpan>();
            var value4 = fixture.Create<TimeSpan>();
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
                        Assert.AreEqual(value1, t);
                        break;
                    case 1:
                        Assert.AreEqual(value2, t);
                        break;
                    case 2:
                        Assert.AreEqual(value3, t);
                        break;
                    case 3:
                        Assert.AreEqual(value4, t);
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
