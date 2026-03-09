using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class JsonExpressionSerializerTests
    {
        [TestMethod]
        public void RoundTripAction()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var value = fixture.Create<int>();
            var test = Create();
            var bytes = test.ConvertMethodToBytes((message, notification) => Tester.SetValue(value));
            var method = test.ConvertBytesToMethod(bytes);
            method.Compile().DynamicInvoke(null, null);
            Assert.AreEqual(Tester.GetValue(), value);
        }


        [TestMethod]
        public void ConvertToBytes_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertMethodToBytes(null);
           });
        }
        [TestMethod]
        public void BytesToMethod_Null_Exception()
        {
            var test = Create();
            Assert.ThrowsExactly<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesToMethod(null);
           });
        }

        private IExpressionSerializer Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<JsonExpressionSerializer>();
        }

        private static class Tester
        {
            private static int _value;
            public static void SetValue(int value)
            {
                _value = value;
            }

            public static int GetValue()
            {
                return _value;
            }

            public static int ReturnValue(int value)
            {
                return value;
            }
        }
    }
}
