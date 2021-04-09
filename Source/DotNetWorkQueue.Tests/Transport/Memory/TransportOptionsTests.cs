using DotNetWorkQueue.Transport.Memory;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    public class TransportOptionsTests
    {
        [Fact()]
        public void ValidConfiguration_Test()
        {
            var options = new TransportOptions();
            var valid = options.ValidConfiguration();
            Assert.True(valid.Valid);
            options.SetReadOnly();
            valid = options.ValidConfiguration();
            Assert.True(valid.Valid);
        }

        [Fact()]
        public void SetReadOnly_Test()
        {
            var options = new TransportOptions();
            Assert.False(options.IsReadOnly);
            options.SetReadOnly();
            Assert.True(options.IsReadOnly);
        }
    }
}