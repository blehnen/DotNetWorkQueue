using System;
using System.Collections.Generic;
using System.Text;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Interceptors;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    [TestClass]
    public class InterceptionTest
    {
        [TestMethod]
        public void Interceptor_Multiple_Interceptors()
        {
            var list = new List<IMessageInterceptor>
            {
                new GZipMessageInterceptor(new GZipMessageInterceptorConfiguration()),
#pragma warning disable CS0618 // deliberately testing the deprecated 3DES interceptor
                new TripleDesMessageInterceptor(
                    new TripleDesMessageInterceptorConfiguration(
                        Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                        Convert.FromBase64String("aaaaaaaaaaa=")))
#pragma warning restore CS0618
            };

            IMessageInterceptorRegistrar register = new MessageInterceptors(list, new InterceptorFactory(Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }
        [TestMethod]
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

        [TestMethod]
        public void Interceptor_Zero_Interceptors()
        {
            IMessageInterceptorRegistrar register = new MessageInterceptors(new List<IMessageInterceptor>(), new InterceptorFactory(Substitute.For<IContainerFactory>()));

            var r = new Random();
            foreach (var body in Helpers.RandomStrings(100000, 1000000, 10, r))
            {
                Test(register, body);
            }
        }

        [TestMethod]
        public void Coexistence_ConsumerPoolDecryptsBoth()
        {
            var aesKey = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(aesKey);
            var aesCfg = new AesMessageInterceptorConfiguration(aesKey);

#pragma warning disable CS0618 // migration scenario: legacy 3DES stays in the consumer pool
            var tdesCfg = new TripleDesMessageInterceptorConfiguration(
                Convert.FromBase64String("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Convert.FromBase64String("aaaaaaaaaaa="));

            // Producer registers AES only
            var producer = new MessageInterceptors(
                new List<IMessageInterceptor> { new AesMessageInterceptor(aesCfg) },
                new InterceptorFactory(Substitute.For<IContainerFactory>()));

            // A legacy producer that used 3DES only
            var legacyProducer = new MessageInterceptors(
                new List<IMessageInterceptor> { new TripleDesMessageInterceptor(tdesCfg) },
                new InterceptorFactory(Substitute.For<IContainerFactory>()));

            // Consumer registers BOTH — the pool of decryptors
            var consumer = new MessageInterceptors(
                new List<IMessageInterceptor> { new AesMessageInterceptor(aesCfg), new TripleDesMessageInterceptor(tdesCfg) },
                new InterceptorFactory(Substitute.For<IContainerFactory>()));
#pragma warning restore CS0618

            var body = Encoding.UTF8.GetBytes("coexistence body");

            var aesMsg = producer.MessageToBytes(body, null);
            CollectionAssert.AreEqual(body, consumer.BytesToMessage(aesMsg.Output, aesMsg.Graph, null));

            var tdesMsg = legacyProducer.MessageToBytes(body, null);
            CollectionAssert.AreEqual(body, consumer.BytesToMessage(tdesMsg.Output, tdesMsg.Graph, null));
        }

        private void Test(IMessageInterceptorRegistrar register, string body)
        {
            var serialization = register.MessageToBytes(Encoding.UTF8.GetBytes(body), null);
            var message = register.BytesToMessage(serialization.Output, serialization.Graph, null);
            Assert.AreEqual(body, Encoding.UTF8.GetString(message));
        }
    }
}
