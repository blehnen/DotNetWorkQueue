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
using DotNetWorkQueue.Serialization;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
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
