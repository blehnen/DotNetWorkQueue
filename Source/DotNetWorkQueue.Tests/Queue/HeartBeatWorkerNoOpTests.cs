using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class HeartBeatWorkerNoOpTests
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

        private IHeartBeatWorker Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<HeartBeatWorkerNoOp>();
        }
    }
}
