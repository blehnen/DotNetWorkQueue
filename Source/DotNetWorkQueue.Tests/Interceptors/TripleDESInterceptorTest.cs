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
using System.Text;
using DotNetWorkQueue.Interceptors;
using Xunit;

namespace DotNetWorkQueue.Tests.Interceptors
{
    public class TripleDesInterceptorTest
    {
        private readonly TripleDesMessageInterceptor _tripleDesMessageInterceptor = new TripleDesMessageInterceptor(new TripleDesMessageInterceptorConfiguration(Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"), Convert.FromBase64String("aaaaaaaaaaa=")));
        private readonly Random _random = new Random();

        [Theory]
        [InlineData(1, 8, 10),
        InlineData(100000, 1000000, 10),
        InlineData(1000000, 1000000, 1)]
        public void ShouldEncryptAndDecrypt(int minLength,
           int maxLength,
           int count)
        {
            foreach (var body in Helpers.RandomStrings(minLength, maxLength, count, _random))
            {
                TestDes(body);
            }
        }
        private void TestDes(string body)
        {
            var serialization = _tripleDesMessageInterceptor.MessageToBytes(Encoding.UTF8.GetBytes(body));
            var actual = Encoding.UTF8.GetString(_tripleDesMessageInterceptor.BytesToMessage(serialization.Output));
            Assert.Equal(body, actual);
        }
    }
}
