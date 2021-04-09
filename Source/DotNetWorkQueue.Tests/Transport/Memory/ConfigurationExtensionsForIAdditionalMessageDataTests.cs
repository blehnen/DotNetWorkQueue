using System;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory
{
    public class ConfigurationExtensionsForIAdditionalMessageDataTests
    {
        [Fact()]
        public void SetDelay_Test()
        {
            var time = DateTime.UtcNow;
            var data = new AdditionalMessageData();
            data.SetDelay(time.TimeOfDay);

            Assert.Equal(time.TimeOfDay, data.GetDelay());
        }

        [Fact()]
        public void GetDelay_Test()
        {
            var data = new AdditionalMessageData();
            Assert.Null(data.GetDelay());

            var time = DateTime.UtcNow;
            data.SetDelay(time.TimeOfDay);
            Assert.Equal(time.TimeOfDay, data.GetDelay());

            time = DateTime.UtcNow.AddHours(1);
            data.SetDelay(time.TimeOfDay);
            Assert.Equal(time.TimeOfDay, data.GetDelay());
        }
    }
}