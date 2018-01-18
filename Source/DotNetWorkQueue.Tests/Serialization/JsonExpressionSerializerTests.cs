using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Serialization;
using Xunit;

namespace DotNetWorkQueue.Tests.Serialization
{
    public class JsonExpressionSerializerTests
    {
        [Theory, AutoData]
        public void RoundTripAction(int value)
        {
            var test = Create();
            var bytes = test.ConvertMethodToBytes((message, notification) => Tester.SetValue(value));
            var method = test.ConvertBytesToMethod(bytes);
            method.Compile().DynamicInvoke(null, null);
            Assert.Equal(Tester.GetValue(), value);
        }

        [Theory, AutoData]
        public void RoundTripFunction(int value)
        {
            var test = Create();
            var bytes = test.ConvertFunctionToBytes((message, notification) => Tester.ReturnValue(value));
            var method = test.ConvertBytesToFunction(bytes);
            var methodValue = (int)method.Compile().DynamicInvoke(null, null);
            Assert.Equal(methodValue, value);

        }

        [Fact]
        public void ConvertToBytes_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertMethodToBytes(null);
           });
        }
        [Fact]
        public void BytesToMethod_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesToMethod(null);
           });
        }

        [Fact]
        public void ConvertFunctionToBytes_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertFunctionToBytes(null);
           });
        }
        [Fact]
        public void BytesToFunction_Null_Exception()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
           delegate
           {
               test.ConvertBytesToFunction(null);
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
