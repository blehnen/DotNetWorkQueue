using Xunit;
using DotNetWorkQueue.Transport.LiteDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.LiteDb.Tests
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

        [Fact()]
        public void SetExpiration_Test()
        {
            var time = DateTime.UtcNow;
            var data = new AdditionalMessageData();
            data.SetExpiration(time.TimeOfDay);

            Assert.Equal(time.TimeOfDay, data.GetExpiration());
        }

        [Fact()]
        public void GetExpiration_Test()
        {
            var data = new AdditionalMessageData();
            Assert.Null(data.GetExpiration());

            var time = DateTime.UtcNow;
            data.SetExpiration(time.TimeOfDay);
            Assert.Equal(time.TimeOfDay, data.GetExpiration());

            time = DateTime.UtcNow.AddHours(1);
            data.SetExpiration(time.TimeOfDay);
            Assert.Equal(time.TimeOfDay, data.GetExpiration());
        }
    }
}