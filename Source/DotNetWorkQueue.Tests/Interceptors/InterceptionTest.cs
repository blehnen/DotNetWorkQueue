// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Text;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Interceptors;
using Xunit;
namespace DotNetWorkQueue.Tests.Interceptors
{
    public class InterceptionTest
    {
        [Fact]
        public void Interceptor_Multiple_Interceptors()
        {
            var list = new List<IMessageInterceptor>
            {
                new GZipMessageInterceptor(new GZipMessageInterceptorConfiguration()),
                new TripleDesMessageInterceptor(
                    new TripleDesMessageInterceptorConfiguration(
                        Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                        Convert.FromBase64String("aaaaaaaaaaa=")))
            };
            
            IMessageInterceptorRegistrar register = new MessageInterceptors(list, new InterceptorFactory(NSubstitute.Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }
        [Fact]
        public void Interceptor_Single_Interceptors()
        {
            var list = new List<IMessageInterceptor> {new GZipMessageInterceptor(new GZipMessageInterceptorConfiguration())};
            IMessageInterceptorRegistrar register = new MessageInterceptors(list, new InterceptorFactory(NSubstitute.Substitute.For<IContainerFactory>()));
            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }

        [Fact]
        public void Interceptor_Zero_Interceptors()
        {
            IMessageInterceptorRegistrar register = new MessageInterceptors(new List<IMessageInterceptor>(), new InterceptorFactory(NSubstitute.Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }

        private void Test(IMessageInterceptorRegistrar register, string body)
        {
            var serialization = register.MessageToBytes(Encoding.UTF8.GetBytes(body));
            var message = register.BytesToMessage(serialization.Output, serialization.Graph);
            Assert.Equal(body, Encoding.UTF8.GetString(message));
        }
    }
}
