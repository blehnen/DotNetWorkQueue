﻿using System;
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
