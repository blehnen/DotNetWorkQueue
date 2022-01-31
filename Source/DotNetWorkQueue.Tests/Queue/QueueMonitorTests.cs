using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class QueueMonitorTests
    {
        [Fact]
        public void Create_Default()
        {
            using (var test = Create())
            {
                test.Start();
                test.Stop();
            }
        }

        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.True(test.IsDisposed);
            }
        }

        [Fact]
        public void Start_Called_Only_Once_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
            delegate
            {
                test.Start();
            });
            }
        }

        [Fact]
        public void Start_Called_After_Dispose_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Start();
            });
            }
        }

        [Fact]
        public void Stop_Called_After_Dispose_Exception()
        {
            using (var test = Create())
            {
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Stop();
            });
            }
        }

        private IQueueMonitor Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var heartBeat = fixture.Create<IHeartBeatConfiguration>();
            heartBeat.Enabled.Returns(true);
            var message = fixture.Create<IMessageExpirationConfiguration>();
            message.Enabled.Returns(true);

            fixture.Inject(heartBeat);
            fixture.Inject(message);

            var transport = fixture.Create<TransportConfigurationReceive>();
            transport.HeartBeatSupported = true;
            transport.MessageExpirationSupported = true;
            fixture.Inject(transport);
            return fixture.Create<QueueMonitor>();
        }
    }
}
