using System;
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Interceptors;
using NSubstitute;
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

            IMessageInterceptorRegistrar register = new MessageInterceptors(list, new InterceptorFactory(Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }
        [Fact]
        public void Interceptor_Single_Interceptors()
        {
            var list = new List<IMessageInterceptor> { new GZipMessageInterceptor(new GZipMessageInterceptorConfiguration()) };
            IMessageInterceptorRegistrar register = new MessageInterceptors(list, new InterceptorFactory(Substitute.For<IContainerFactory>()));
            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }

        [Fact]
        public void Interceptor_Zero_Interceptors()
        {
            IMessageInterceptorRegistrar register = new MessageInterceptors(new List<IMessageInterceptor>(), new InterceptorFactory(Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }

        private void Test(IMessageInterceptorRegistrar register, string body)
        {
            var serialization = register.MessageToBytes(Encoding.UTF8.GetBytes(body), null);
            var message = register.BytesToMessage(serialization.Output, serialization.Graph, null);
            Assert.Equal(body, Encoding.UTF8.GetString(message));
        }
    }
}
