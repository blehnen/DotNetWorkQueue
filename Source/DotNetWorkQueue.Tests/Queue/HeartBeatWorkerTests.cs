using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;
using NSubstitute.ExceptionExtensions;


using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class HeartBeatWorkerTests
    {
        [TestMethod]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.IsFalse(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [TestMethod]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [TestMethod]
        public void Call_Stop_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Stop();
                test.Stop();
            }
        }

        [TestMethod]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.IsTrue(test.IsDisposed);
        }

        [TestMethod]
        public void Disposed_Instance_Start_Exception()
        {
            var test = Create();
            test.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(
                delegate
                {
                    test.Start();
                });
        }

        [TestMethod]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.ThrowsExactly<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [TestMethod]
        public void Test_Send_Exception()
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();

            sendHeartBeat.Send(context).Throws(new ArgumentOutOfRangeException());

            using (var test = Create(TimeSpan.FromSeconds(5), "sec(*%2)", context, sendHeartBeat))
            {
                test.Start();
                Thread.Sleep(7000);
            }
        }

        [TestMethod]
        [DataRow(5),
         DataRow(59),
         DataRow(65),
         DataRow(600),
         DataRow(6000),
         DataRow(60000)]
        public void Test_SendDiff(int seconds)
        {
            var sendHeartBeat = Substitute.For<ISendHeartBeat>();
            var context = Substitute.For<IMessageContext>();
            using (var test = Create(TimeSpan.FromSeconds(seconds), "sec(*%2)", context, sendHeartBeat))
            {
                test.Start();
                Thread.Sleep(1100);
            }
        }

        private HeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return Create(TimeSpan.Zero, "sec(*%59)", fixture.Create<IMessageContext>(), fixture.Create<ISendHeartBeat>());
        }


        private HeartBeatWorker Create(TimeSpan checkSpan, string updateTime, IMessageContext context, ISendHeartBeat sendHeartBeat)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(context);
            fixture.Inject(sendHeartBeat);
            var threadPoolConfiguration = fixture.Create<IHeartBeatThreadPoolConfiguration>();
            threadPoolConfiguration.ThreadsMax.Returns(1);
            fixture.Inject(threadPoolConfiguration);
            IHeartBeatConfiguration configuration = fixture.Create<HeartBeatConfiguration>();
            configuration.Time = checkSpan;
            configuration.UpdateTime = updateTime;
            fixture.Inject(configuration);
            var threadpool = fixture.Create<IHeartBeatScheduler>();
            fixture.Inject(threadpool);
            return fixture.Create<HeartBeatWorker>();
        }
    }
}
